using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.PriceHistory;

public class PriceSnapshotJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PriceSnapshotJob> _logger;

    public PriceSnapshotJob(IServiceScopeFactory scopeFactory, ILogger<PriceSnapshotJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Stagger startup so it doesn't compete with feed ingestion
        await Task.Delay(TimeSpan.FromMinutes(5), ct);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));

        await TakeSnapshotsAsync(ct);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await TakeSnapshotsAsync(ct);
        }
    }

    private async Task TakeSnapshotsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            await using var db = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<DealFinderDbContext>>()
                .CreateDbContext();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var catalogIds = await db.ProductCatalog
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(ct);

            var inserted = 0;

            foreach (var catalogId in catalogIds)
            {
                // Skip if we already snapshotted today
                var alreadyExists = await db.PriceSnapshots
                    .AnyAsync(s => s.CatalogProductId == catalogId && s.RecordedDate == today, ct);

                if (alreadyExists) continue;

                var minPrice = await db.ProductListings
                    .Where(l => l.ProductCatalogId == catalogId && l.Price > 0)
                    .Select(l => (decimal?)(l.Price + l.ShippingPrice))
                    .MinAsync(ct);

                if (minPrice is null) continue;

                db.PriceSnapshots.Add(new PriceSnapshot
                {
                    CatalogProductId = catalogId,
                    MinPrice = minPrice.Value,
                    RecordedDate = today,
                });

                inserted++;
            }

            await db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Price snapshot complete. Date={Date}, Inserted={Inserted}",
                today, inserted
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Price snapshot job failed.");
        }
    }
}
