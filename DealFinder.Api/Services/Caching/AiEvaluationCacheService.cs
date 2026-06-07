using System.Security.Cryptography;
using System.Text;
using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.Caching;

public class AiEvaluationCacheService : IAiEvaluationCacheService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    private readonly IDbContextFactory<DealFinderDbContext> _dbContextFactory;

    public AiEvaluationCacheService(
        IDbContextFactory<DealFinderDbContext> dbContextFactory
    )
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> TryApplyCachedEvaluationAsync(ProductResult product)
    {
        if (string.IsNullOrWhiteSpace(product.Url))
        {
            return false;
        }

        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var urlHash = HashUrl(product.Url);

        var cached = await dbContext.AiEvaluationCaches
            .FirstOrDefaultAsync(x => x.UrlHash == urlHash);

        if (cached is null)
        {
            return false;
        }

        var isFresh =
            cached.CreatedAtUtc >= DateTime.UtcNow.Subtract(CacheLifetime);

        var samePrice =
            cached.Price == product.Price &&
            cached.ShippingPrice == product.ShippingPrice;

        if (!isFresh || !samePrice)
        {
            return false;
        }

        product.Score = cached.Score;
        product.Summary = cached.Summary;
        product.Condition = cached.Condition;

        Console.WriteLine($"CACHE HIT: {product.Title}");

        return true;
    }

    public async Task SaveEvaluationAsync(ProductResult product)
    {
        if (string.IsNullOrWhiteSpace(product.Url))
        {
            return;
        }

        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var urlHash = HashUrl(product.Url);

        var existing = await dbContext.AiEvaluationCaches
            .FirstOrDefaultAsync(x => x.UrlHash == urlHash);

        if (existing is null)
        {
            dbContext.AiEvaluationCaches.Add(new AiEvaluationCache
            {
                Url = product.Url,
                UrlHash = urlHash,
                Title = product.Title,
                Price = product.Price,
                ShippingPrice = product.ShippingPrice,
                Score = product.Score,
                Summary = product.Summary,
                Condition = product.Condition,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Title = product.Title;
            existing.Price = product.Price;
            existing.ShippingPrice = product.ShippingPrice;
            existing.Score = product.Score;
            existing.Summary = product.Summary;
            existing.Condition = product.Condition;
            existing.CreatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine($"CACHE SAVED: {product.Title}");
    }

    private static string HashUrl(string url)
    {
        var normalizedUrl = url.Trim().ToLowerInvariant();

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(normalizedUrl)
        );

        return Convert.ToHexString(bytes);
    }
}