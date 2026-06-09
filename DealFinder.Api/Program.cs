using DealFinder.Api.Data;
using DealFinder.Api.Data.Seed;
using DealFinder.Api.Middleware;
using DealFinder.Api.Providers;
using DealFinder.Api.Services;
using DealFinder.Api.Services.Caching;
using DealFinder.Api.Services.Catalog;
using DealFinder.Api.Services.OpenAI;
using DealFinder.Api.Services.Pricing;
using DealFinder.Api.Services.Scoring;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using DealFinder.Api.Services.Feed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContextFactory<DealFinderDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                {
                    return false;
                }

                var uri = new Uri(origin);

                return
                    uri.Host == "localhost" ||
                    uri.Host.EndsWith(".vercel.app");
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("SearchPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(10);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddHttpClient<IOpenAiService, OpenAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddScoped<IProductProvider, DbProductProvider>();
builder.Services.AddScoped<IProductSearchService, ProductSearchService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IAiEvaluationCacheService, AiEvaluationCacheService>();

builder.Services.AddScoped<IListingPromptBuilder, CategoryPromptBuilder>();
builder.Services.AddScoped<IProductScorer, ProductScorer>();
builder.Services.AddScoped<IPreFilterScorer, PreFilterScorer>();
builder.Services.AddScoped<IPriceEstimator, PriceEstimator>();
builder.Services.AddScoped<IQueryIntentParser, QueryIntentParser>();
builder.Services.AddScoped<IAwinFeedIngestionService, AwinFeedIngestionService>();
builder.Services.AddHostedService<AwinFeedRefreshJob>();

builder.Services.AddHttpClient("AwinFeeds", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});



var app = builder.Build();

// Explicit CORS headers for Vercel/frontend.
// This is intentionally before all other middleware so even successful 200 responses
// include Access-Control-Allow-Origin.
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString();

    if (!string.IsNullOrWhiteSpace(origin))
    {
        var isAllowedOrigin = false;

        try
        {
            var originUri = new Uri(origin);

            isAllowedOrigin =
                originUri.Host == "localhost" ||
                originUri.Host.EndsWith(".vercel.app");
        }
        catch
        {
            isAllowedOrigin = false;
        }

        if (isAllowedOrigin)
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Vary"] = "Origin";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        }
    }

    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    await next();
});

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
    await ProductListingSeeder.SeedAsync(dbContext);
}

app.MapControllers();

app.Run();