using DealFinder.Api.Models;
using DealFinder.Api.Services.Classification;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Data.Seed;

public static class ProductListingSeeder
{
    public static async Task SeedAsync(DealFinderDbContext dbContext)
    {
        if (await dbContext.ProductListings.AnyAsync())
        {
            return;
        }

        var listings = new List<ProductListing>
        {
            new ProductListing
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
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Gpu
            },

            new ProductListing
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
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Gpu
            },

            new ProductListing
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
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Gpu
            },

            new ProductListing
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
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Gpu
            },

            new ProductListing
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
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Gpu
            },

            new ProductListing
            {
                Title = "RTX 3080 10GB - no display, for parts or repair",
                Description = "Card powers on but gives no display. Sold for parts or repair only. No returns.",
                Price = 95,
                ShippingPrice = 8,
                Condition = "For parts",
                SellerName = "RepairPartsUK",
                SellerFeedbackPercentage = 96.0,
                SellerFeedbackScore = 214,
                Marketplace = "MockBay",
                Url = "https://example.com/rtx-3080-no-display",
                ImageUrl = "https://images.unsplash.com/photo-1591489378430-ef2f4c626b35?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Gpu
            },

            new ProductListing
            {
                Title = "AMD Ryzen 5 5600X - used, working",
                Description = "Used in a gaming PC for around 2 years. Works fine, no bent pins. Includes original box but no cooler.",
                Price = 85,
                ShippingPrice = 4,
                Condition = "Used",
                SellerName = "PCPartsUK",
                SellerFeedbackPercentage = 98.9,
                SellerFeedbackScore = 620,
                Marketplace = "MockBay",
                Url = "https://example.com/ryzen-5600x-used",
                ImageUrl = "https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Cpu
            },

            new ProductListing
            {
                Title = "Intel Core i7-12700K - tested working",
                Description = "Pulled from my personal workstation after upgrade. Tested and working. No original box. Some thermal paste residue.",
                Price = 185,
                ShippingPrice = 6,
                Condition = "Used",
                SellerName = "TechClearout",
                SellerFeedbackPercentage = 99.1,
                SellerFeedbackScore = 1140,
                Marketplace = "MockBay",
                Url = "https://example.com/i7-12700k-tested",
                ImageUrl = "https://images.unsplash.com/photo-1555617981-dac3880eac6e?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Cpu
            },

            new ProductListing
            {
                Title = "Ryzen 7 5800X - possible bent pin, sold as seen",
                Description = "CPU was working before removal but one pin may be slightly bent. I have not retested it. Sold as seen, no returns.",
                Price = 70,
                ShippingPrice = 5,
                Condition = "For parts",
                SellerName = "BudgetBuilds",
                SellerFeedbackPercentage = 95.2,
                SellerFeedbackScore = 91,
                Marketplace = "MockBay",
                Url = "https://example.com/ryzen-5800x-bent-pin",
                ImageUrl = "https://images.unsplash.com/photo-1555617981-dac3880eac6e?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Cpu
            },

            new ProductListing
            {
                Title = "Intel Core i5-12400F - boxed with stock cooler",
                Description = "Lightly used CPU. Includes original box and Intel stock cooler. Fully working, only upgraded to i7.",
                Price = 95,
                ShippingPrice = 0,
                Condition = "Used",
                SellerName = "GamingPartsLondon",
                SellerFeedbackPercentage = 100,
                SellerFeedbackScore = 320,
                Marketplace = "MockBay",
                Url = "https://example.com/i5-12400f-boxed",
                ImageUrl = "https://images.unsplash.com/photo-1591799264318-7e6ef8ddb7ea?q=80&w=1200&auto=format&fit=crop",
                Category = ProductCategory.Cpu
            }
        };

        dbContext.ProductListings.AddRange(listings);
        await dbContext.SaveChangesAsync();
    }
}