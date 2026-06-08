using DealFinder.Api.Models;
using DealFinder.Api.Services.Classification;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Data.Seed;

public static class ProductListingSeeder
{
    public static async Task SeedAsync(DealFinderDbContext dbContext)
    {
        await SeedCatalogAsync(dbContext);

        if (await dbContext.ProductListings.AnyAsync())
        {
            return;
        }

        var rtx3080 = await GetCatalogProductAsync(dbContext, "RTX 3080");
        var rtx3080Ti = await GetCatalogProductAsync(dbContext, "RTX 3080 Ti");
        var ryzen5600x = await GetCatalogProductAsync(dbContext, "Ryzen 5 5600X");
        var ryzen5800x = await GetCatalogProductAsync(dbContext, "Ryzen 7 5800X");
        var i512400f = await GetCatalogProductAsync(dbContext, "Intel Core i5-12400F");
        var i712700k = await GetCatalogProductAsync(dbContext, "Intel Core i7-12700K");

        var listings = new List<ProductListing>
        {
            new ProductListing
            {
                ProductCatalogId = rtx3080.Id,
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
                ProductCatalogId = rtx3080.Id,
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
                ProductCatalogId = rtx3080.Id,
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
                ProductCatalogId = rtx3080.Id,
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
                ProductCatalogId = rtx3080Ti.Id,
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
                ProductCatalogId = rtx3080.Id,
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
                ProductCatalogId = ryzen5600x.Id,
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
                ProductCatalogId = i712700k.Id,
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
                ProductCatalogId = ryzen5800x.Id,
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
                ProductCatalogId = i512400f.Id,
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

    private static async Task SeedCatalogAsync(DealFinderDbContext dbContext)
    {
        if (await dbContext.ProductCatalog.AnyAsync())
        {
            return;
        }

        var catalog = new List<ProductCatalog>
        {
            new ProductCatalog
            {
                Category = ProductCategory.Gpu,
                Label = "RTX 3080",
                SearchQuery = "RTX 3080",
                Description = "High-end 1440p / entry 4K GPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Gpu,
                Label = "RTX 3080 Ti",
                SearchQuery = "RTX 3080 Ti",
                Description = "Faster 3080-class GPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Gpu,
                Label = "RTX 3070",
                SearchQuery = "RTX 3070",
                Description = "Strong 1440p GPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Gpu,
                Label = "RTX 2080",
                SearchQuery = "RTX 2080",
                Description = "Older high-end NVIDIA GPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Gpu,
                Label = "RTX 3060 Ti",
                SearchQuery = "RTX 3060 Ti",
                Description = "Good budget 1080p / 1440p card",
                IsActive = true
            },

            new ProductCatalog
            {
                Category = ProductCategory.Cpu,
                Label = "Ryzen 5 5600X",
                SearchQuery = "Ryzen 5 5600X",
                Description = "AM4 gaming CPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Cpu,
                Label = "Ryzen 7 5800X",
                SearchQuery = "Ryzen 7 5800X",
                Description = "8-core AM4 CPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Cpu,
                Label = "Ryzen 5 7600",
                SearchQuery = "Ryzen 5 7600",
                Description = "AM5 gaming CPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Cpu,
                Label = "Intel Core i5-12400F",
                SearchQuery = "Intel Core i5-12400F",
                Description = "Budget Intel gaming CPU",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Cpu,
                Label = "Intel Core i7-12700K",
                SearchQuery = "Intel Core i7-12700K",
                Description = "High-performance Intel CPU",
                IsActive = true
            },

            new ProductCatalog
            {
                Category = ProductCategory.Ram,
                Label = "16GB DDR4 3200MHz",
                SearchQuery = "16GB DDR4 3200MHz RAM",
                Description = "Basic DDR4 gaming memory",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Ram,
                Label = "32GB DDR4 3600MHz",
                SearchQuery = "32GB DDR4 3600MHz RAM",
                Description = "Good AM4 DDR4 kit",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Ram,
                Label = "32GB DDR5 6000MHz",
                SearchQuery = "32GB DDR5 6000MHz RAM",
                Description = "Good AM5 / modern Intel DDR5 kit",
                IsActive = true
            },

            new ProductCatalog
            {
                Category = ProductCategory.Psu,
                Label = "650W Gold PSU",
                SearchQuery = "650W Gold PSU",
                Description = "For mid-range builds",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Psu,
                Label = "750W Gold PSU",
                SearchQuery = "750W Gold PSU",
                Description = "Good for most gaming PCs",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Psu,
                Label = "850W Gold PSU",
                SearchQuery = "850W Gold PSU",
                Description = "For higher-end GPU builds",
                IsActive = true
            },

            new ProductCatalog
            {
                Category = ProductCategory.Motherboard,
                Label = "B550 AM4 Motherboard",
                SearchQuery = "B550 AM4 motherboard",
                Description = "Ryzen 3000/5000 DDR4 board",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Motherboard,
                Label = "B650 AM5 Motherboard",
                SearchQuery = "B650 AM5 motherboard",
                Description = "Ryzen 7000/9000 DDR5 board",
                IsActive = true
            },
            new ProductCatalog
            {
                Category = ProductCategory.Motherboard,
                Label = "Z790 LGA1700 Motherboard",
                SearchQuery = "Z790 LGA1700 motherboard",
                Description = "Intel 12th/13th/14th gen board",
                IsActive = true
            }
        };

        dbContext.ProductCatalog.AddRange(catalog);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<ProductCatalog> GetCatalogProductAsync(
        DealFinderDbContext dbContext,
        string label
    )
    {
        var product = await dbContext.ProductCatalog
            .FirstOrDefaultAsync(x => x.Label == label);

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Catalog product '{label}' was not found during seeding."
            );
        }

        return product;
    }
}