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

        var points = await db.PriceSnapshots
            .Where(s => s.CatalogProductId == catalogId)
            .OrderBy(s => s.RecordedDate)
            .TakeLast(30)
            .Select(s => new PriceHistoryPointDto
            {
                Date = s.RecordedDate.ToString("yyyy-MM-dd"),
                MinPrice = s.MinPrice
            })
            .ToListAsync();

        return Ok(points);
    }
}
