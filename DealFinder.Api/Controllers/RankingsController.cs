using DealFinder.Api.Models;
using DealFinder.Api.Services.Rankings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RankingsController : ControllerBase
{
    private readonly IRankingsService _rankingsService;

    public RankingsController(IRankingsService rankingsService)
    {
        _rankingsService = rankingsService;
    }

    [HttpGet]
    [EnableRateLimiting("SearchPolicy")]
    public async Task<ActionResult<List<RankingItemDto>>> GetRankings()
    {
        var rankings = await _rankingsService.GetTopDealsAsync(perCategory: 20);
        return Ok(rankings);
    }
}
