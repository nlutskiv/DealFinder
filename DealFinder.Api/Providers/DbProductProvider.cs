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

    public async Task<List<ProductResult>> SearchProductsAsync(int catalogProductId)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        // Filter in the database (ProductCatalogId is indexed) instead of pulling
        // the whole listings table into memory and filtering in C#.
        var listings = await dbContext.ProductListings
            .AsNoTracking()
            .Where(listing => listing.ProductCatalogId == catalogProductId)
            .OrderBy(listing => listing.Price)
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