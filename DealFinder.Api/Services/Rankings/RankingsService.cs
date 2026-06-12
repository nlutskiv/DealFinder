using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.Rankings;

public class RankingsService : IRankingsService
{
    private static readonly Dictionary<string, string> CategoryLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gpu"] = "GPU",
        ["cpu"] = "CPU",
        ["motherboard"] = "Motherboard",
        ["ram"] = "RAM",
        ["storage"] = "Storage",
        ["psu"] = "PSU",
        ["case"] = "Case",
        ["cooler"] = "CPU Cooler",
    };

    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;

    public RankingsService(IDbContextFactory<DealFinderDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<RankingItemDto>> GetTopDealsAsync(int perCategory = 5)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // One query per category to get the top N cheapest models
        var topModels = new List<TopModel>();

        foreach (var (category, categoryLabel) in CategoryLabels)
        {
            var rows = await db.ProductCatalog
                .Where(c => c.Category.ToLower() == category && c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.Label,
                    MinPrice = db.ProductListings
                        .Where(l => l.ProductCatalogId == c.Id && l.Price > 0)
                        .Select(l => (decimal?)(l.Price + l.ShippingPrice))
                        .Min(),
                    ListingCount = db.ProductListings
                        .Count(l => l.ProductCatalogId == c.Id && l.Price > 0),
                })
                .Where(x => x.MinPrice != null)
                .OrderBy(x => x.MinPrice)
                .Take(perCategory)
                .ToListAsync();

            foreach (var r in rows)
                topModels.Add(new TopModel(category, categoryLabel, r.Id, r.Label, r.MinPrice!.Value, r.ListingCount));
        }

        if (topModels.Count == 0) return [];

        // Single query to get the cheapest listing for every matched catalog product
        var catalogIds = topModels.Select(m => m.CatalogId).ToList();

        var allListings = await db.ProductListings
            .Where(l => l.ProductCatalogId.HasValue
                        && catalogIds.Contains(l.ProductCatalogId!.Value)
                        && l.Price > 0)
            .Select(l => new
            {
                CatalogId = l.ProductCatalogId!.Value,
                l.Url,
                l.ImageUrl,
                l.Marketplace,
                Total = l.Price + l.ShippingPrice,
            })
            .ToListAsync();

        // Pick cheapest per catalog product in memory
        var topListingByCatalog = allListings
            .GroupBy(l => l.CatalogId)
            .ToDictionary(g => g.Key, g => g.MinBy(l => l.Total)!);

        var results = new List<RankingItemDto>(topModels.Count);

        foreach (var m in topModels)
        {
            if (!topListingByCatalog.TryGetValue(m.CatalogId, out var top)) continue;

            results.Add(new RankingItemDto
            {
                Category = m.Category,
                CategoryLabel = m.CategoryLabel,
                ModelLabel = m.Label,
                CatalogProductId = m.CatalogId,
                MinPrice = m.MinPrice,
                ListingCount = m.ListingCount,
                TopListingUrl = top.Url,
                TopListingImageUrl = top.ImageUrl,
                TopListingMarketplace = top.Marketplace,
            });
        }

        return results;
    }

    private record TopModel(
        string Category,
        string CategoryLabel,
        int CatalogId,
        string Label,
        decimal MinPrice,
        int ListingCount);
}
