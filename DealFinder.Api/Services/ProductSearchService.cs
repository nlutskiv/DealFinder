using DealFinder.Api.Models;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.Catalog;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly IEnumerable<IProductProvider> _providers;
    private readonly IProductCatalogService _catalogService;

    public ProductSearchService(
        IEnumerable<IProductProvider> providers,
        IProductCatalogService catalogService
    )
    {
        _providers = providers;
        _catalogService = catalogService;
    }

    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var catalogProduct = await _catalogService.GetByIdAsync(request.CatalogProductId);

        if (catalogProduct is null)
        {
            return [];
        }

        // Providers filter by catalog id in the database, so no in-memory filtering needed.
        var allResults = new List<ProductResult>();

        foreach (var provider in _providers)
        {
            allResults.AddRange(await provider.SearchProductsAsync(request.CatalogProductId));
        }

        return allResults
            .OrderBy(p => p.Price)
            .ToList();
    }
}
