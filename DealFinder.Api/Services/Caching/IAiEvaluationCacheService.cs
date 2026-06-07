using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Caching;

public interface IAiEvaluationCacheService
{
    Task<bool> TryApplyCachedEvaluationAsync(ProductResult product);
    Task SaveEvaluationAsync(ProductResult product);
}