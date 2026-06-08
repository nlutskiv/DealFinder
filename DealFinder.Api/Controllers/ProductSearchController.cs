using DealFinder.Api.Exceptions;
using DealFinder.Api.Models;
using DealFinder.Api.Services;
using DealFinder.Api.Services.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductSearchController : ControllerBase
{
    private readonly IProductSearchService _service;
    private readonly IProductCatalogService _catalogService;

    public ProductSearchController(
        IProductSearchService service,
        IProductCatalogService catalogService
    )
    {
        _service = service;
        _catalogService = catalogService;
    }

    [EnableRateLimiting("SearchPolicy")]
    [HttpPost("search")]
    public async Task<ActionResult<List<ProductResult>>> Search(
        ProductSearchRequest request
    )
    {
        if (request.CatalogProductId <= 0)
        {
            throw new AppException(
                "Please choose a product first.",
                "INVALID_PRODUCT_SELECTION",
                StatusCodes.Status400BadRequest
            );
        }

        var catalogProduct =
            await _catalogService.GetByIdAsync(request.CatalogProductId);

        if (catalogProduct is null)
        {
            throw new AppException(
                "Unsupported product selection.",
                "UNSUPPORTED_PRODUCT",
                StatusCodes.Status400BadRequest
            );
        }

        var results = await _service.SearchAsync(request);

        return Ok(results);
    }
}