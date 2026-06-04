using DealFinder.Api.Models;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        await Task.Delay(500);

        return new List<ProductResult>
        {
            new ProductResult
            {
                Title = $"{request.Query} - Amazon",
                Price = 499.99m,
                Marketplace = "Amazon",
                Url = "https://amazon.com",
                Score = 8.7
            },

            new ProductResult
            {
                Title = $"{request.Query} - Ebay",
                Price = 479.99m,
                Marketplace = "Ebay",
                Url = "https://ebay.com",
                Score = 8.1
            }
        };
    }
}   