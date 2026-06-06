using DealFinder.Api.Models;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.OpenAI;
using DealFinder.Api.Services.Pricing;
using DealFinder.Api.Services.Scoring;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private const int MaxCandidatesForAiEvaluation = 30;
    private const int MaxConcurrentAiCalls = 5;

    private readonly IEnumerable<IProductProvider> _providers;
    private readonly IOpenAiService _openAiService;
    private readonly IGpuPromptBuilder _gpuPromptBuilder;
    private readonly IProductScorer _productScorer;
    private readonly IPriceEstimator _priceEstimator;
    private readonly IPreFilterScorer _preFilterScorer;
    private readonly IQueryIntentParser _queryIntentParser;

    public ProductSearchService(
        IEnumerable<IProductProvider> providers,
        IOpenAiService openAiService,
        IGpuPromptBuilder gpuPromptBuilder,
        IProductScorer productScorer,
        IPriceEstimator priceEstimator,
        IPreFilterScorer preFilterScorer,
        IQueryIntentParser queryIntentParser
    )
    {
        _providers = providers;
        _openAiService = openAiService;
        _gpuPromptBuilder = gpuPromptBuilder;
        _productScorer = productScorer;
        _priceEstimator = priceEstimator;
        _preFilterScorer = preFilterScorer;
        _queryIntentParser = queryIntentParser;
    }

    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var allResults = await FetchAllProviderResultsAsync(request.Query);

        var estimatedMeanPrice = _priceEstimator.EstimateMeanPrice(
            request.Query,
            allResults
        );

        var intent = _queryIntentParser.Parse(request.Query);

        var candidates = allResults
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
        var prompt = _gpuPromptBuilder.BuildEvaluationPrompt(
            request,
            product,
            estimatedMeanPrice
        );

        var evaluation = await _openAiService.EvaluateGpuListingAsync(prompt);

        product.Score = _productScorer.CalculateScore(evaluation);
        product.Summary = evaluation.Reasoning;
        product.Condition = evaluation.Condition;

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