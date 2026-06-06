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
                Title = "RTX 3080 10GB MSI Gaming X Trio - used for mining but works perfectly",
                Description = "Used for ETH mining for around 1 year. Always undervolted and kept in a ventilated rig. No crashes or artifacting in my testing, but no warranty left.",
                Price = 320,
                ShippingPrice = 12,
                Condition = "Used",
                SellerName = "GPUOutletUK",
                SellerFeedbackPercentage = 97.8,
                SellerFeedbackScore = 842,
                Marketplace = "MockBay",
                Url = "https://example.com/rtx-3080-msi-mining",
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop"
            },

            new ProductResult
            {
                Title = "NVIDIA RTX 3080 Founders Edition 10GB - tested working",
                Description = "Personal gaming card, never mined on. Tested in Cyberpunk and 3DMark last week. Runs fine, but has some cosmetic scratches on the cooler.",
                Price = 385,
                ShippingPrice = 8,
                Condition = "Used",
                SellerName = "LondonPCParts",
                SellerFeedbackPercentage = 99.2,
                SellerFeedbackScore = 1240,
                Marketplace = "MockBay",
                Url = "https://example.com/rtx-3080-fe-tested",
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop"
            },

            new ProductResult
            {
                Title = "Gigabyte RTX 3080 10GB - works but fan sometimes noisy",
                Description = "Card works and displays fine. Used mostly for gaming. One fan can get a bit noisy under load, so might need cleaning or replacing eventually.",
                Price = 295,
                ShippingPrice = 10,
                Condition = "Used",
                SellerName = "BudgetBuilds",
                SellerFeedbackPercentage = 96.4,
                SellerFeedbackScore = 318,
                Marketplace = "MockBay",
                Url = "https://example.com/gigabyte-rtx-3080-noisy-fan",
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop"
            },

            new ProductResult
            {
                Title = "ASUS TUF RTX 3080 OC 10GB - no returns",
                Description = "Selling my ASUS TUF 3080. Works fine as far as I know. I upgraded recently and it has been sitting in a box for a few months. Sold as seen, no returns.",
                Price = 340,
                ShippingPrice = 15,
                Condition = "Used",
                SellerName = "TechClearout",
                SellerFeedbackPercentage = 94.1,
                SellerFeedbackScore = 76,
                Marketplace = "MockBay",
                Url = "https://example.com/asus-tuf-3080-no-returns",
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop"
            },

            new ProductResult
            {
                Title = "RTX 3080 Ti 12GB EVGA FTW3 Ultra - excellent condition",
                Description = "EVGA RTX 3080 Ti, used in my personal PC. Never mined. Includes original box and purchase receipt. Ran Heaven benchmark with no artifacting.",
                Price = 515,
                ShippingPrice = 0,
                Condition = "Used",
                SellerName = "HighEndHardware",
                SellerFeedbackPercentage = 100,
                SellerFeedbackScore = 562,
                Marketplace = "MockBay",
                Url = "https://example.com/evga-rtx-3080-ti",
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop"
            }
        };
    }
}