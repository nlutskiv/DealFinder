using DealFinder.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;
    private readonly IMemoryCache _cache;

    public StatsController(IDbContextFactory<DealFinderDbContext> dbFactory, IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    [HttpGet]
    [EnableRateLimiting("SearchPolicy")]
    public async Task<IActionResult> Get()
    {
        var stats = await _cache.GetOrCreateAsync("site-stats", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);

            await using var db = await _dbFactory.CreateDbContextAsync();

            var listingCount = await db.ProductListings.CountAsync(l => l.Price > 0);
            var modelCount = await db.ProductCatalog.CountAsync(c => c.IsActive);
            var retailerCount = await db.ProductListings
                .Where(l => l.Marketplace != "")
                .Select(l => l.Marketplace)
                .Distinct()
                .CountAsync();
            var lastUpdatedUtc = await db.ProductListings
                .Select(l => (DateTime?)l.LastSeenUtc)
                .MaxAsync();

            return new
            {
                listingCount,
                modelCount,
                retailerCount,
                lastUpdatedUtc,
            };
        });

        return Ok(stats);
    }
}
