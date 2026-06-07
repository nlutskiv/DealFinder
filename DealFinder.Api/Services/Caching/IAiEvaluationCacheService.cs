using DealFinder.Api.Models;
using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.Caching;

public interface IAiEvaluationCacheService
{
    Task<ListingEvaluationResult?> GetFreshAnalysisAsync(ProductResult product);

    Task SaveAnalysisAsync(
        ProductResult product,
        ListingEvaluationResult evaluation
    );
}