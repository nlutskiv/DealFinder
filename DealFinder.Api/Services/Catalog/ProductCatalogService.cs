using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DealFinder.Api.Services.Catalog;

public class ProductCatalogService : IProductCatalogService
{
    private const string CacheKey = "catalog:active";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly IDbContextFactory<DealFinderDbContext> _dbContextFactory;
    private readonly IMemoryCache _cache;

    public ProductCatalogService(
        IDbContextFactory<DealFinderDbContext> dbContextFactory,
        IMemoryCache cache
    )
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
    }

    public async Task<List<CatalogProductDto>> GetActiveCatalogAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<CatalogProductDto>? cached) && cached is not null)
            return cached;

        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var result = await dbContext.ProductCatalog
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

        _cache.Set(CacheKey, result, CacheTtl);
        return result;
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