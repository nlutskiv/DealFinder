using DealFinder.Api.Models;

namespace DealFinder.Api.Providers;

public interface IProductProvider
{
    Task<List<ProductResult>> SearchProductsAsync(string query);
}