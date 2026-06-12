using DealFinder.Api.Models.PcBuild;
using DealFinder.Api.Services.PcBuild;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PcBuildController : ControllerBase
{
    private readonly IPcBuildService _pcBuildService;
    private readonly ILogger<PcBuildController> _logger;

    public PcBuildController(IPcBuildService pcBuildService, ILogger<PcBuildController> logger)
    {
        _pcBuildService = pcBuildService;
        _logger = logger;
    }

    [HttpPost("generate")]
    [EnableRateLimiting("AiPolicy")]
    public async Task<ActionResult<PcBuildResult>> Generate([FromBody] PcBuildRequest request)
    {
        if (request.Budget <= 0)
        {
            return BadRequest(new { message = "Budget must be a positive number." });
        }

        _logger.LogInformation("PC build request received. Budget=£{Budget}", request.Budget);

        try
        {
            var result = await _pcBuildService.GenerateBuildAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            _logger.LogError("OpenAI API key is not configured.");
            return StatusCode(503, new { message = "AI service is not configured. Contact support." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PC build generation failed.");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
