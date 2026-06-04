using Microsoft.AspNetCore.Mvc;
using DealFinder.Api.Models;
using DealFinder.Api.Services;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductSearchController : ControllerBase
{
    // service implemenation
    private readonly IProductSearchService _service;

    public ProductSearchController(IProductSearchService service)
    {
        _service = service;
    }

    //api/ProductSearch/search
    [HttpPost("search")]
    public async Task<ActionResult<List<ProductResult>>> Search(ProductSearchRequest request)
    {
        var results = await _service.SearchAsync(request);
        return Ok(results);
    }
}