using DealFinder.Api.Models;
using DealFinder.Api.Providers;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly IEnumerable<IProductProvider> _providers;

    public ProductSearchService(IEnumerable<IProductProvider> providers)
    {
        _providers = providers;
    }

    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var allResults = new List<ProductResult>();

        foreach (var provider in _providers)
        {
            var results = await provider.SearchProductsAsync(request.Query);
            allResults.AddRange(results);
        }

        return allResults
            .OrderByDescending(x => x.Score)
            .ToList();
    }
}