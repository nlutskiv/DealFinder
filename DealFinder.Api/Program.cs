using DealFinder.Api.Data;
using DealFinder.Api.Middleware;
using DealFinder.Api.Providers;
using DealFinder.Api.Services;
using DealFinder.Api.Services.Catalog;
using DealFinder.Api.Services.OpenAI;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using DealFinder.Api.Services.Email;
using DealFinder.Api.Services.Feed;
using DealFinder.Api.Services.PcBuild;
using DealFinder.Api.Services.PriceAlert;
using DealFinder.Api.Services.PriceHistory;
using DealFinder.Api.Services.Rankings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Trust the X-Forwarded-For header from Railway's reverse proxy so that
// rate limiting uses the real client IP, not the proxy's IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Clear known-network restrictions so we accept the header from any upstream proxy.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContextFactory<DealFinderDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Single, authoritative CORS policy — covers localhost, Vercel previews, and the custom domain.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                try
                {
                    var uri = new Uri(origin);
                    return
                        uri.Host == "localhost" ||
                        uri.Host.StartsWith("localhost:") ||
                        uri.Host.EndsWith(".vercel.app") ||
                        uri.Host == "dealfinder.tech" ||
                        uri.Host == "www.dealfinder.tech";
                }
                catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Resolve the real client IP after ForwardedHeaders middleware has run.
    static string ClientIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";

    // 60 requests/10 min per IP for DB-only endpoints (catalog + search)
    options.AddPolicy("SearchPolicy", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        )
    );

    // 5 AI-backed PC builds per IP per 10 minutes
    options.AddPolicy("AiPolicy", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        )
    );

    // 10 price alert registrations per IP per hour
    options.AddPolicy("AlertPolicy", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientIp(ctx),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        )
    );
});

builder.Services.AddHttpClient<IOpenAiService, OpenAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});

builder.Services.AddScoped<IProductProvider, DbProductProvider>();
builder.Services.AddScoped<IProductSearchService, ProductSearchService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IPcBuildService, PcBuildService>();

builder.Services.AddScoped<IAwinFeedIngestionService, AwinFeedIngestionService>();
builder.Services.AddHostedService<AwinFeedRefreshJob>();

builder.Services.AddHttpClient("AwinFeeds", client =>
{
    client.Timeout = TimeSpan.FromMinutes(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36"
    );
});

builder.Services.AddScoped<IRankingsService, RankingsService>();

builder.Services.AddHostedService<PriceSnapshotJob>();
builder.Services.AddHostedService<PriceAlertCheckJob>();

builder.Services.AddHttpClient<IEmailService, EmailService>();

var app = builder.Build();

// Must be first — rewrites RemoteIpAddress from X-Forwarded-For before rate limiting reads it.
app.UseForwardedHeaders();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<DealFinderDbContext>>();

    await using var dbContext = await dbFactory.CreateDbContextAsync();

    await dbContext.Database.MigrateAsync();

    await PerformanceScoreSeeder.ApplyAsync(dbContext);
}

app.MapControllers();

app.Run();
