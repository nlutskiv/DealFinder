using DealFinder.Api.Services;
using DealFinder.Api.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddScoped<IProductSearchService, ProductSearchService>();
builder.Services.AddScoped<IProductProvider, MockProductProvider>();
builder.Services.AddScoped<IProductProvider, EbayProductProvider>();

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