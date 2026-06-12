using DealFinder.Api.Data;
using DealFinder.Api.Models;
using DealFinder.Api.Models.PcBuild;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuildsController : ControllerBase
{
    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;

    public BuildsController(IDbContextFactory<DealFinderDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpPost]
    [EnableRateLimiting("AiPolicy")]
    public async Task<ActionResult<object>> SaveBuild([FromBody] SaveBuildRequest request)
    {
        if (request.Parts is null || request.Parts.Count == 0)
            return BadRequest(new { message = "Build must have at least one part." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var id = Guid.NewGuid().ToString("N")[..10];

        db.SavedBuilds.Add(new SavedBuild
        {
            Id = id,
            Budget = request.Budget,
            TotalPrice = request.TotalPrice,
            AiSummary = request.AiSummary ?? "",
            PartsJson = JsonConvert.SerializeObject(request.Parts),
            CreatedAtUtc = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();

        return Ok(new { id });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PcBuildResult>> GetBuild(string id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var saved = await db.SavedBuilds.FindAsync(id);
        if (saved is null) return NotFound(new { message = "Build not found." });

        var parts = JsonConvert.DeserializeObject<List<PcBuildPart>>(saved.PartsJson) ?? [];

        return Ok(new PcBuildResult
        {
            Budget = saved.Budget,
            TotalPrice = saved.TotalPrice,
            AiSummary = saved.AiSummary,
            Parts = parts,
            CompatibilityWarnings = [],
            IsCompatible = true,
        });
    }
}

public class SaveBuildRequest
{
    public decimal Budget { get; set; }
    public decimal TotalPrice { get; set; }
    public string? AiSummary { get; set; }
    public List<PcBuildPart> Parts { get; set; } = [];
}
