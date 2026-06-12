using DealFinder.Api.Models;
using DealFinder.Api.Services.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IProductCatalogService _catalogService;

    public CatalogController(IProductCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [EnableRateLimiting("SearchPolicy")]
    public async Task<ActionResult<List<CatalogProductDto>>> GetCatalog()
    {
        var catalog = await _catalogService.GetActiveCatalogAsync();

        return Ok(catalog);
    }
}