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
                Title = "RTX 3080 10GB MSI Gaming X Trio - used for mining but works perfectly",
                Description = "Used for ETH mining for 1 year. Temps always good.",
                Price = 320,
                Marketplace = "MockBay"
            },

            new ProductResult
            {
                Title = "RTX 3080 FE faulty no display for parts",
                Description = "Sometimes artifacts. Selling as spares or repair.",
                Price = 120,
                Marketplace = "MockBay"
            },

            new ProductResult
            {
                Title = "RTX 3080 ASUS TUF excellent condition boxed",
                Description = "Never mined on. Gaming only.",
                Price = 410,
                Marketplace = "MockBay"
            }
        };
    }
}