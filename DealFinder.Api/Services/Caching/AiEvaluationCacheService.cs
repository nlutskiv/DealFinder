using System.Security.Cryptography;
using System.Text;
using DealFinder.Api.Data;
using DealFinder.Api.Models;
using DealFinder.Api.Models.OpenAI;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

    public async Task<ListingEvaluationResult?> GetFreshAnalysisAsync(ProductResult product)
    {
        if (string.IsNullOrWhiteSpace(product.Url))
        {
            return null;
        }

        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var urlHash = HashText(product.Url);

        var cached = await dbContext.AiEvaluationCaches
            .FirstOrDefaultAsync(cache => cache.UrlHash == urlHash);

        if (cached is null)
        {
            return null;
        }

        var isFresh =
            cached.CreatedAtUtc >= DateTime.UtcNow.Subtract(CacheLifetime);

        var samePrice =
            cached.Price == product.Price &&
            cached.ShippingPrice == product.ShippingPrice;

        if (!isFresh || !samePrice)
        {
            return null;
        }

        Console.WriteLine($"CACHE HIT: {product.Title}");

        return new ListingEvaluationResult
        {
            Model = "cached",
            VRamGb = null,

            // Query/value/final score are recalculated per search.
            QueryMatchScore = 0,
            ValueScore = 0,

            Condition = cached.Condition,
            ConditionScore = cached.ConditionScore,
            SellerTrustScore = cached.SellerTrustScore,
            RiskScore = cached.RiskScore,
            RiskLabel = cached.RiskLabel,
            Reasoning = cached.AnalysisSummary,

            DetectedIssues =
                JsonConvert.DeserializeObject<List<string>>(cached.DetectedIssuesJson)
                ?? new List<string>(),

            PositiveSignals =
                JsonConvert.DeserializeObject<List<string>>(cached.PositiveSignalsJson)
                ?? new List<string>()
        };
    }

    public async Task SaveAnalysisAsync(
        ProductResult product,
        ListingEvaluationResult evaluation
    )
    {
        if (string.IsNullOrWhiteSpace(product.Url))
        {
            return;
        }

        await using var dbContext =
            await _dbContextFactory.CreateDbContextAsync();

        var urlHash = HashText(product.Url);

        var existing = await dbContext.AiEvaluationCaches
            .FirstOrDefaultAsync(cache => cache.UrlHash == urlHash);

        if (existing is null)
        {
            dbContext.AiEvaluationCaches.Add(new AiEvaluationCache
            {
                Url = product.Url,
                UrlHash = urlHash,
                Title = product.Title,
                Price = product.Price,
                ShippingPrice = product.ShippingPrice,

                Condition = evaluation.Condition,
                ConditionScore = evaluation.ConditionScore,
                SellerTrustScore = evaluation.SellerTrustScore,
                RiskScore = evaluation.RiskScore,
                RiskLabel = evaluation.RiskLabel,

                DetectedIssuesJson =
                    JsonConvert.SerializeObject(evaluation.DetectedIssues),

                PositiveSignalsJson =
                    JsonConvert.SerializeObject(evaluation.PositiveSignals),

                AnalysisSummary = evaluation.Reasoning,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.Title = product.Title;
            existing.Price = product.Price;
            existing.ShippingPrice = product.ShippingPrice;

            existing.Condition = evaluation.Condition;
            existing.ConditionScore = evaluation.ConditionScore;
            existing.SellerTrustScore = evaluation.SellerTrustScore;
            existing.RiskScore = evaluation.RiskScore;
            existing.RiskLabel = evaluation.RiskLabel;

            existing.DetectedIssuesJson =
                JsonConvert.SerializeObject(evaluation.DetectedIssues);

            existing.PositiveSignalsJson =
                JsonConvert.SerializeObject(evaluation.PositiveSignals);

            existing.AnalysisSummary = evaluation.Reasoning;
            existing.CreatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine($"CACHE SAVED: {product.Title}");
    }

    private static string HashText(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(normalized)
        );

        return Convert.ToHexString(bytes);
    }
}