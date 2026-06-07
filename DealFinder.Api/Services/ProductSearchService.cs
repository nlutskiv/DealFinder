using DealFinder.Api.Models;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.Classification;
using DealFinder.Api.Services.OpenAI;
using DealFinder.Api.Services.Pricing;
using DealFinder.Api.Services.Scoring;
using DealFinder.Api.Services.Caching;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private const int MaxCandidatesForAiEvaluation = 30;
    private const int MaxConcurrentAiCalls = 5;

    private readonly IEnumerable<IProductProvider> _providers;
    private readonly IOpenAiService _openAiService;
    private readonly IListingPromptBuilder _listingPromptBuilder;
    private readonly IProductScorer _productScorer;
    private readonly IPriceEstimator _priceEstimator;
    private readonly IPreFilterScorer _preFilterScorer;
    private readonly IQueryIntentParser _queryIntentParser;
    private readonly IAiEvaluationCacheService _cacheService;

    public ProductSearchService(
        IEnumerable<IProductProvider> providers,
        IOpenAiService openAiService,
        IListingPromptBuilder listingPromptBuilder,
        IProductScorer productScorer,
        IPriceEstimator priceEstimator,
        IPreFilterScorer preFilterScorer,
        IQueryIntentParser queryIntentParser,
        IAiEvaluationCacheService cacheService
    )
    {
        _providers = providers;
        _openAiService = openAiService;
        _listingPromptBuilder = listingPromptBuilder;
        _productScorer = productScorer;
        _priceEstimator = priceEstimator;
        _preFilterScorer = preFilterScorer;
        _queryIntentParser = queryIntentParser;
        _cacheService = cacheService;
    }

    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var category = ProductCategoryDetector.Detect(request.Query);

        if (category == ProductCategory.Generic)
        {
            return new List<ProductResult>();
        }

        var allResults = await FetchAllProviderResultsAsync(request.Query);

        Console.WriteLine($"Detected category: {category}");
        Console.WriteLine($"All results count: {allResults.Count}");

        foreach (var product in allResults)
        {
            Console.WriteLine($"Product: {product.Title} | Category: '{product.Category}'");
        }

        var categoryResults = FilterByCategory(allResults, category);

        Console.WriteLine($"Category results count: {categoryResults.Count}");

        if (categoryResults.Count == 0)
        {
            return new List<ProductResult>();
        }


        var estimatedMeanPrice = _priceEstimator.EstimateMeanPrice(
            request.Query,
            categoryResults
        );

        var intent = _queryIntentParser.Parse(request.Query);

        var candidates = SelectCandidates(
            request,
            categoryResults,
            estimatedMeanPrice,
            intent
        );

        await EvaluateCandidatesWithAiAsync(
            request,
            candidates,
            estimatedMeanPrice
        );

        return candidates
            .OrderByDescending(product => product.Score)
            .ToList();
    }

    private async Task<List<ProductResult>> FetchAllProviderResultsAsync(string query)
    {
        var allResults = new List<ProductResult>();

        foreach (var provider in _providers)
        {
            var results = await provider.SearchProductsAsync(query);
            allResults.AddRange(results);
        }

        return allResults;
    }


    private static List<ProductResult> FilterByCategory(
        List<ProductResult> products,
        string category
    )
    {
        return products
            .Where(product => product.Category == category)
            .ToList();
    }

    private List<ProductResult> SelectCandidates(
        ProductSearchRequest request,
        List<ProductResult> products,
        decimal? estimatedMeanPrice,
        SearchIntent intent
    )
    {
        return products
            .OrderByDescending(product =>
                _preFilterScorer.Score(
                    request.Query,
                    product,
                    estimatedMeanPrice,
                    intent
                )
            )
            .Take(MaxCandidatesForAiEvaluation)
            .ToList();
    }

    private async Task EvaluateCandidatesWithAiAsync(
        ProductSearchRequest request,
        List<ProductResult> candidates,
        decimal? estimatedMeanPrice
    )
    {
        using var semaphore = new SemaphoreSlim(MaxConcurrentAiCalls);

        var evaluationTasks = candidates.Select(async product =>
        {
            await semaphore.WaitAsync();

            try
            {
                await EvaluateSingleProductAsync(
                    request,
                    product,
                    estimatedMeanPrice
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"AI evaluation failed for '{product.Title}': {ex.Message}"
                );

                product.Score = 0;
                product.Summary = "AI evaluation failed.";
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(evaluationTasks);
    }

    private async Task EvaluateSingleProductAsync(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    )
    {
        var cacheHit = await _cacheService.TryApplyCachedEvaluationAsync(product);

        if (cacheHit)
        {
            return;
        }

        var prompt = _listingPromptBuilder.BuildEvaluationPrompt(
            request,
            product,
            estimatedMeanPrice
        );

        var evaluation = await _openAiService.EvaluateListingAsync(prompt);

        product.Score = _productScorer.CalculateScore(evaluation);
        product.Summary = evaluation.Reasoning;
        product.Condition = evaluation.Condition;

        await _cacheService.SaveEvaluationAsync(product);

        Console.WriteLine(
            $"Model: {evaluation.Model}, " +
            $"Query: {evaluation.QueryMatchScore}, " +
            $"ConditionScore: {evaluation.ConditionScore}, " +
            $"SellerTrust: {evaluation.SellerTrustScore}, " +
            $"Risk: {evaluation.RiskScore}, " +
            $"Value: {evaluation.ValueScore}, " +
            $"Final: {product.Score}, " +
            $"Reasoning: {evaluation.Reasoning}"
        );
    }
}