using System.Text.RegularExpressions;
using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DealFinder.Api.Services.Rankings;

public class RankingsService : IRankingsService
{
    private const string CacheKey = "rankings:v4";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    // Sellers treated as "risky" (used/refurb marketplaces). Keep in sync with the
    // frontend list in models/retailer-trust.ts. Matched as a case-insensitive substring.
    private static readonly string[] RiskyMarketplaces =
    [
        "love tech hate waste",
        "lovetechhatewaste",
    ];

    private static bool IsRiskyMarketplace(string marketplace)
    {
        var m = (marketplace ?? "").Trim().ToLowerInvariant();
        return RiskyMarketplaces.Any(r => m.Contains(r));
    }

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

        // Never cache an empty result — otherwise a request that lands during a
        // truncate/re-ingest poisons the cache with [] for the whole TTL, leaving
        // rankings blank even after the DB is repopulated.
        if (results.Count > 0)
        {
            _cache.Set(CacheKey, results, CacheTtl);
        }

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
                .ToListAsync();

            var candidates = rows
                .Select(r =>
                {
                    // Merit = the "more is better" spec for the category:
                    //   GPU/CPU -> benchmark score, Storage/RAM -> capacity (GB),
                    //   PSU -> wattage. ValueScore = merit per pound.
                    var merit = ModelMerit(category, r.Label, r.PerformanceScore);
                    var valueScore = merit.HasValue && r.MinPrice!.Value > 0
                        ? Math.Round(merit.Value / (double)r.MinPrice.Value, 4)
                        : (double?)null;

                    return new TopModel(
                        category, categoryLabel, r.Id, r.Label,
                        r.MinPrice!.Value, r.ListingCount,
                        r.PerformanceScore, valueScore);
                })
                .ToList();

            // Categories with a measurable spec (GPU/CPU/Storage/RAM/PSU) rank by
            // value-per-pound — best bang per pound first, not the cheapest tiny part.
            // Motherboard/Case/Cooler have no such axis, so cheapest-first stands.
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

        // Cheapest listing per model from a non-risky seller only.
        var trustedTopByCatalog = allListings
            .Where(l => !IsRiskyMarketplace(l.Marketplace))
            .GroupBy(l => l.CatalogId)
            .ToDictionary(g => g.Key, g => g.MinBy(l => l.Total)!);

        var results = new List<RankingItemDto>(topModels.Count);

        foreach (var m in topModels)
        {
            if (!topListingByCatalog.TryGetValue(m.CatalogId, out var top)) continue;

            var hasTrusted = trustedTopByCatalog.TryGetValue(m.CatalogId, out var trusted);

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
                TrustedMinPrice = hasTrusted ? trusted!.Total : null,
                TrustedTopListingUrl = hasTrusted ? trusted!.Url : "",
                TrustedTopListingImageUrl = hasTrusted ? trusted!.ImageUrl : "",
                TrustedTopListingMarketplace = hasTrusted ? trusted!.Marketplace : "",
                PerformanceScore = m.PerformanceScore,
                ValueScore = m.ValueScore,
            });
        }

        return results;
    }

    // The "more is better" spec used to rank value within a category.
    private static double? ModelMerit(string category, string label, double? performanceScore) =>
        category switch
        {
            "gpu" or "cpu" => performanceScore,        // benchmark index
            "storage" or "ram" => ParseCapacityGb(label),
            "psu" => ParseWatts(label),
            _ => null,                                   // motherboard/case/cooler: cheapest-first
        };

    private static double? ParseCapacityGb(string label)
    {
        var m = Regex.Match(label, @"(\d+)\s?(gb|tb)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        var n = double.Parse(m.Groups[1].Value);
        return m.Groups[2].Value.Equals("tb", StringComparison.OrdinalIgnoreCase) ? n * 1000 : n;
    }

    private static double? ParseWatts(string label)
    {
        var m = Regex.Match(label, @"(\d{3,4})\s?w", RegexOptions.IgnoreCase);
        return m.Success ? double.Parse(m.Groups[1].Value) : null;
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
