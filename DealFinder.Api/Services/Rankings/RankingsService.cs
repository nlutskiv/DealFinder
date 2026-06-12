using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DealFinder.Api.Services.Rankings;

public class RankingsService : IRankingsService
{
    private const string CacheKey = "rankings:v2";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

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
    private readonly IMemoryCache _cache;

    public RankingsService(IDbContextFactory<DealFinderDbContext> dbFactory, IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    public async Task<List<RankingItemDto>> GetTopDealsAsync(int perCategory = 5)
    {
        if (_cache.TryGetValue(CacheKey, out List<RankingItemDto>? cached) && cached is not null)
        {
            return cached;
        }

        var results = await BuildRankingsAsync(perCategory);

        _cache.Set(CacheKey, results, CacheTtl);

        return results;
    }

    private async Task<List<RankingItemDto>> BuildRankingsAsync(int perCategory)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var topModels = new List<TopModel>();

        foreach (var (category, categoryLabel) in CategoryLabels)
        {
            // Pull a wider candidate pool so value ranking has something to choose from.
            var rows = await db.ProductCatalog
                .Where(c => c.Category.ToLower() == category && c.IsActive)
                .Select(c => new
                {
                    c.Id,
                    c.Label,
                    c.PerformanceScore,
                    MinPrice = db.ProductListings
                        .Where(l => l.ProductCatalogId == c.Id && l.Price > 0)
                        .Select(l => (decimal?)(l.Price + l.ShippingPrice))
                        .Min(),
                    ListingCount = db.ProductListings
                        .Count(l => l.ProductCatalogId == c.Id && l.Price > 0),
                })
                .Where(x => x.MinPrice != null)
                .OrderBy(x => x.MinPrice)
                .Take(40)
                .ToListAsync();

            var candidates = rows
                .Select(r => new TopModel(
                    category, categoryLabel, r.Id, r.Label,
                    r.MinPrice!.Value, r.ListingCount,
                    r.PerformanceScore,
                    r.PerformanceScore.HasValue && r.MinPrice.Value > 0
                        ? Math.Round(r.PerformanceScore.Value / (double)r.MinPrice.Value, 3)
                        : null))
                .ToList();

            // GPU/CPU: rank by performance-per-pound (scored models first), so the top
            // of the list is the best value, not the cheapest obsolete part.
            // Other categories have no performance axis — cheapest first remains correct.
            var ordered = candidates.Any(c => c.ValueScore.HasValue)
                ? candidates
                    .OrderByDescending(c => c.ValueScore.HasValue)
                    .ThenByDescending(c => c.ValueScore)
                    .ThenBy(c => c.MinPrice)
                : candidates.OrderBy(c => c.MinPrice);

            topModels.AddRange(ordered.Take(perCategory));
        }

        if (topModels.Count == 0) return [];

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
                PerformanceScore = m.PerformanceScore,
                ValueScore = m.ValueScore,
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
        int ListingCount,
        double? PerformanceScore,
        double? ValueScore);
}
