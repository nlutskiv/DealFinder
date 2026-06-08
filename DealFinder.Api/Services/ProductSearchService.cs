using DealFinder.Api.Models;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.Classification;
using DealFinder.Api.Services.OpenAI;
using DealFinder.Api.Services.Pricing;
using DealFinder.Api.Services.Scoring;
using DealFinder.Api.Services.Caching;
using DealFinder.Api.Services.Catalog;

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
    private readonly IProductCatalogService _catalogService;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(
        IEnumerable<IProductProvider> providers,
        IOpenAiService openAiService,
        IListingPromptBuilder listingPromptBuilder,
        IProductScorer productScorer,
        IPriceEstimator priceEstimator,
        IPreFilterScorer preFilterScorer,
        IQueryIntentParser queryIntentParser,
        IAiEvaluationCacheService cacheService,
        IProductCatalogService catalogService,
        ILogger<ProductSearchService> logger
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
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var catalogProduct =
            await _catalogService.GetByIdAsync(request.CatalogProductId);

        if (catalogProduct is null)
        {
            return new List<ProductResult>();
        }

        request.Query = catalogProduct.SearchQuery;

        var category = catalogProduct.Category;

        var allResults = await FetchAllProviderResultsAsync(request.Query);

        var categoryResults = allResults
            .Where(product =>
                product.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
            )
            .Where(product =>
                product.ProductCatalogId == request.CatalogProductId
            )
            .ToList();

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
                _logger.LogWarning(
                    ex,
                    "AI evaluation failed for listing {Title}",
                    product.Title
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
        var evaluation =
            await _cacheService.GetFreshAnalysisAsync(product);

        if (evaluation is null)
        {
            var prompt = _listingPromptBuilder.BuildEvaluationPrompt(
                request,
                product,
                estimatedMeanPrice
            );

            evaluation = await _openAiService.EvaluateListingAsync(prompt);

            await _cacheService.SaveAnalysisAsync(product, evaluation);
        }

        // Recalculate query-dependent fields every search.
        evaluation.QueryMatchScore = QueryMatchScorer.Calculate(
            request.Query,
            product
        );
        evaluation.ValueScore = ValueScorer.Calculate(
            product,
            estimatedMeanPrice,
            evaluation.RiskScore
        );

        product.Score = _productScorer.CalculateScore(evaluation);
        product.Summary = evaluation.Reasoning;
        product.Condition = evaluation.Condition;

        _logger.LogInformation(
            "Evaluated listing {Title}. Query={QueryMatch}, Value={Value}, Condition={ConditionScore}, SellerTrust={SellerTrust}, Risk={Risk}, Final={Final}",
            product.Title,
            evaluation.QueryMatchScore,
            evaluation.ValueScore,
            evaluation.ConditionScore,
            evaluation.SellerTrustScore,
            evaluation.RiskScore,
            product.Score
        );
    }
    
}