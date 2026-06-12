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

        request.Query = catalogProduct.SearchQuery;

        var allResults = await FetchAllProviderResultsAsync(request.Query);

        return allResults
            .Where(p => p.Category.Equals(catalogProduct.Category, StringComparison.OrdinalIgnoreCase))
            .Where(p => p.ProductCatalogId == request.CatalogProductId)
            .OrderBy(p => p.Price)
            .ToList();
    }

    private async Task<List<ProductResult>> FetchAllProviderResultsAsync(string query)
    {
        var results = new List<ProductResult>();

        foreach (var provider in _providers)
        {
            results.AddRange(await provider.SearchProductsAsync(query));
        }

        return results;
    }
}
