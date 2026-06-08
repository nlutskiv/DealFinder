using DealFinder.Api.Models;
using DealFinder.Api.Services.Catalog;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<List<CatalogProductDto>>> GetCatalog()
    {
        var catalog = await _catalogService.GetActiveCatalogAsync();

        return Ok(catalog);
    }
}