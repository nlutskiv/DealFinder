using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Providers;

public class DbProductProvider : IProductProvider
{
    private readonly IDbContextFactory<DealFinderDbContext> _dbContextFactory;

    public DbProductProvider(
        IDbContextFactory<DealFinderDbContext> dbContextFactory
    )
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<ProductResult>> SearchProductsAsync(string query)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var listings = await dbContext.ProductListings
            .AsNoTracking()
            .ToListAsync();

        return listings
            .Select(ToProductResult)
            .ToList();
    }

    private static ProductResult ToProductResult(ProductListing listing)
    {
        return new ProductResult
        {
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            ShippingPrice = listing.ShippingPrice,
            Condition = listing.Condition,
            SellerName = listing.SellerName,
            SellerFeedbackPercentage = listing.SellerFeedbackPercentage,
            SellerFeedbackScore = listing.SellerFeedbackScore,
            Marketplace = listing.Marketplace,
            Url = listing.Url,
            ImageUrl = listing.ImageUrl,
            Category = listing.Category,
            ProductCatalogId = listing.ProductCatalogId
        };
    }
}