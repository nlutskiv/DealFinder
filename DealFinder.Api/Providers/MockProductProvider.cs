using DealFinder.Api.Models;

namespace DealFinder.Api.Providers;

public class MockProductProvider : IProductProvider
{
    public async Task<List<ProductResult>> SearchProductsAsync(string query)
    {
        await Task.Delay(500);

        return new List<ProductResult>
        {
            new ProductResult
            {
                Title = $"{query} - Amazon",
                Price = 499.99m,
                Marketplace = "Amazon",
                Url = "https://amazon.com",
                Score = 8.7
            },

            new ProductResult
            {
                Title = $"{query} - Ebay",
                Price = 479.99m,
                Marketplace = "Ebay",
                Url = "https://ebay.com",
                Score = 8.1
            }
        };
    }
}