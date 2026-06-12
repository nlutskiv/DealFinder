using DealFinder.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealsController : ControllerBase
{
    private const int MinHistoryDays = 5;
    private const double MinDiscountPercent = 5;
    private const int MaxDeals = 8;

    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;
    private readonly IMemoryCache _cache;

    public DealsController(IDbContextFactory<DealFinderDbContext> dbFactory, IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    /// <summary>
    /// Products whose current best price is meaningfully below their 30-day average.
    /// Empty until enough snapshot history accumulates.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("SearchPolicy")]
    public async Task<IActionResult> Get()
    {
        var deals = await _cache.GetOrCreateAsync("hot-deals", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

            await using var db = await _dbFactory.CreateDbContextAsync();

            var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

            var history = await db.PriceSnapshots
                .Where(s => s.RecordedDate >= since)
                .GroupBy(s => s.CatalogProductId)
                .Select(g => new
                {
                    Id = g.Key,
                    Avg = g.Average(x => x.MinPrice),
                    Days = g.Count(),
                })
                .Where(x => x.Days >= MinHistoryDays)
                .ToListAsync();

            if (history.Count == 0) return new List<HotDeal>();

            var avgById = history.ToDictionary(h => h.Id, h => h.Avg);
            var ids = avgById.Keys.ToList();

            var current = await db.ProductListings
                .Where(l => l.ProductCatalogId.HasValue
                            && ids.Contains(l.ProductCatalogId!.Value)
                            && l.Price > 0)
                .GroupBy(l => l.ProductCatalogId!.Value)
                .Select(g => new { Id = g.Key, Min = g.Min(x => x.Price + x.ShippingPrice) })
                .ToListAsync();

            var info = await db.ProductCatalog
                .Where(c => ids.Contains(c.Id) && c.IsActive)
                .Select(c => new { c.Id, c.Label, c.Category })
                .ToDictionaryAsync(c => c.Id);

            var result = new List<HotDeal>();

            foreach (var c in current)
            {
                if (!avgById.TryGetValue(c.Id, out var avg) || avg <= 0) continue;
                if (!info.TryGetValue(c.Id, out var product)) continue;

                var discount = (double)((avg - c.Min) / avg) * 100;
                if (discount < MinDiscountPercent) continue;

                result.Add(new HotDeal(
                    c.Id,
                    product.Label,
                    product.Category,
                    c.Min,
                    Math.Round(avg, 2),
                    Math.Round(discount)
                ));
            }

            return result
                .OrderByDescending(d => d.DiscountPercent)
                .Take(MaxDeals)
                .ToList();
        });

        return Ok(deals);
    }

    public record HotDeal(
        int CatalogProductId,
        string ModelLabel,
        string Category,
        decimal CurrentPrice,
        decimal AveragePrice,
        double DiscountPercent);
}
