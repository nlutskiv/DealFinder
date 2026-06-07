using DealFinder.Api.Services;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.OpenAI;
using DealFinder.Api.Services.Scoring;
using DealFinder.Api.Services.Scoring;
using DealFinder.Api.Services.Pricing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddScoped<IProductSearchService, ProductSearchService>();
builder.Services.AddScoped<IProductProvider, MockProductProvider>();
builder.Services.AddScoped<IProductProvider, EbayProductProvider>();
builder.Services.AddScoped<IListingPromptBuilder, ListingPromptBuilder>();
builder.Services.AddScoped<IProductScorer, ProductScorer>();
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<IPriceEstimator, PriceEstimator>();
builder.Services.AddScoped<IPreFilterScorer, PreFilterScorer>();
builder.Services.AddScoped<IQueryIntentParser, QueryIntentParser>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

app.Run();