using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PriceHistoryController : ControllerBase
{
    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;

    public PriceHistoryController(IDbContextFactory<DealFinderDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet("{catalogId:int}")]
    [EnableRateLimiting("SearchPolicy")]
    public async Task<ActionResult<List<PriceHistoryPointDto>>> GetHistory(int catalogId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // TakeLast is not translatable to SQL — take newest 30 descending, then restore order.
        var points = await db.PriceSnapshots
            .Where(s => s.CatalogProductId == catalogId)
            .OrderByDescending(s => s.RecordedDate)
            .Take(30)
            .Select(s => new PriceHistoryPointDto
            {
                Date = s.RecordedDate.ToString("yyyy-MM-dd"),
                MinPrice = s.MinPrice
            })
            .ToListAsync();

        points.Reverse();

        return Ok(points);
    }
}
