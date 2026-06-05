using DealFinder.Api.Models;

namespace DealFinder.Api.Providers;

public class EbayProductProvider : IProductProvider
{
    public async Task<List<ProductResult>> SearchProductsAsync(string query)
    {
        await Task.Delay(500);

        return new List<ProductResult>
        {
            new ProductResult
            {
                Title = $"{query} - ebay",
                Price = 455.99m,
                Marketplace = "ebay",
                Url = "https://ebay.com",
                Score = 2
            }
        };
    }
}