using DealFinder.Api.Data;
using DealFinder.Api.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.PriceAlert;

public class PriceAlertCheckJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceAlertCheckJob> _logger;

    public PriceAlertCheckJob(IServiceScopeFactory scopeFactory, ILogger<PriceAlertCheckJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Offset from snapshot job so they don't both hammer DB at the same time
        await Task.Delay(TimeSpan.FromMinutes(10), ct);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        await CheckAlertsAsync(ct);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await CheckAlertsAsync(ct);
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await using var db = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<DealFinderDbContext>>()
                .CreateDbContext();

            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var alerts = await db.PriceAlerts
                .Where(a => a.IsActive)
                .ToListAsync(ct);

            var triggered = 0;

            foreach (var alert in alerts)
            {
                var currentMin = await db.ProductListings
                    .Where(l => l.ProductCatalogId == alert.CatalogProductId && l.Price > 0)
                    .Select(l => (decimal?)(l.Price + l.ShippingPrice))
                    .MinAsync(ct);

                if (currentMin is null || currentMin > alert.TargetPrice)
                    continue;

                // Get product label + best deal URL
                var catalog = await db.ProductCatalog.FindAsync([alert.CatalogProductId], ct);
                var topListing = await db.ProductListings
                    .Where(l => l.ProductCatalogId == alert.CatalogProductId && l.Price > 0)
                    .OrderBy(l => l.Price + l.ShippingPrice)
                    .Select(l => new { l.Url })
                    .FirstOrDefaultAsync(ct);

                if (catalog is null || topListing is null) continue;

                await emailService.SendPriceAlertAsync(
                    alert.Email,
                    catalog.Label,
                    alert.TargetPrice,
                    currentMin.Value,
                    topListing.Url,
                    ct
                );

                alert.IsActive = false;
                alert.TriggeredAtUtc = DateTime.UtcNow;
                triggered++;
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Price alert check complete. Triggered={Triggered}", triggered);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Price alert check job failed.");
        }
    }
}
