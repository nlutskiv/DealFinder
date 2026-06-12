using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.Catalog;

public class ProductCatalogService : IProductCatalogService
{
    private readonly IDbContextFactory<DealFinderDbContext> _dbContextFactory;

    public ProductCatalogService(
        IDbContextFactory<DealFinderDbContext> dbContextFactory
    )
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<CatalogProductDto>> GetActiveCatalogAsync()
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ProductCatalog
            .AsNoTracking()
            .Where(product => product.IsActive)
            .Select(product => new CatalogProductDto
            {
                Id = product.Id,
                Category = product.Category,
                Label = product.Label,

                ListingCount = dbContext.ProductListings.Count(listing =>
                    listing.ProductCatalogId == product.Id
                ),

                MinPrice = dbContext.ProductListings
                    .Where(l => l.ProductCatalogId == product.Id)
                    .Select(l => (decimal?)(l.Price + l.ShippingPrice))
                    .Min(),

                MaxPrice = dbContext.ProductListings
                    .Where(l => l.ProductCatalogId == product.Id)
                    .Select(l => (decimal?)(l.Price + l.ShippingPrice))
                    .Max()
            })
            .Where(product => product.ListingCount > 0)
            .OrderBy(product => product.Category)
            .ThenBy(product => product.Label)
            .ToListAsync();
    }

    public async Task<ProductCatalog?> GetByIdAsync(int id)
    {
        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        return await dbContext.ProductCatalog
            .AsNoTracking()
            .FirstOrDefaultAsync(product =>
                product.Id == id &&
                product.IsActive
            );
    }
}