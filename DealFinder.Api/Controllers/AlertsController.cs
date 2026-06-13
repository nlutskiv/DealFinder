using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;

    public AlertsController(IDbContextFactory<DealFinderDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    [HttpGet]
    [EnableRateLimiting("AlertPolicy")]
    public async Task<IActionResult> GetAlerts([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "A valid email address is required." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var alerts = await db.PriceAlerts
            .Where(a => a.Email == email && a.IsActive)
            .Join(db.ProductCatalog,
                a => a.CatalogProductId,
                c => c.Id,
                (a, c) => new AlertDto
                {
                    Id = a.Id,
                    ProductName = c.Label,
                    Category = c.Category,
                    TargetPrice = a.TargetPrice,
                    CreatedAtUtc = a.CreatedAtUtc
                })
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();

        return Ok(alerts);
    }

    [HttpDelete("{id}")]
    [EnableRateLimiting("AlertPolicy")]
    public async Task<IActionResult> DeleteAlert(int id, [FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return BadRequest(new { message = "A valid email address is required." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var alert = await db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == id && a.Email == email && a.IsActive);
        if (alert is null)
            return NotFound();

        alert.IsActive = false;
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost]
    [EnableRateLimiting("AlertPolicy")]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { message = "A valid email address is required." });

        if (request.TargetPrice <= 0)
            return BadRequest(new { message = "Target price must be greater than zero." });

        await using var db = await _dbFactory.CreateDbContextAsync();

        var catalogExists = await db.ProductCatalog.AnyAsync(c => c.Id == request.CatalogProductId);
        if (!catalogExists)
            return NotFound(new { message = "Product not found." });

        var currentMin = await db.ProductListings
            .Where(l => l.ProductCatalogId == request.CatalogProductId && l.Price > 0)
            .Select(l => (decimal?)(l.Price + l.ShippingPrice))
            .MinAsync();

        if (currentMin.HasValue && request.TargetPrice >= currentMin.Value)
            return BadRequest(new { message = $"Target price must be below the current low of £{Math.Floor(currentMin.Value)}." });

        // Deactivate any existing alert for same email + product so we don't spam
        var existing = await db.PriceAlerts
            .Where(a => a.Email == request.Email && a.CatalogProductId == request.CatalogProductId && a.IsActive)
            .ToListAsync();

        foreach (var old in existing)
            old.IsActive = false;

        db.PriceAlerts.Add(new PriceAlert
        {
            Email = request.Email,
            CatalogProductId = request.CatalogProductId,
            TargetPrice = request.TargetPrice,
        });

        await db.SaveChangesAsync();

        return Ok();
    }
}

public class CreateAlertRequest
{
    public string Email { get; set; } = "";
    public int CatalogProductId { get; set; }
    public decimal TargetPrice { get; set; }
}

public class AlertDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal TargetPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
