using DealFinder.Api.Models;
using DealFinder.Api.Models.OpenAI;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.OpenAI;
using DealFinder.Api.Services.Scoring;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly IEnumerable<IProductProvider> _providers;
    private readonly IOpenAiService _openAiService;
    private readonly IGpuPromptBuilder _gpuPromptBuilder;
    private readonly IProductScorer _productScorer;


    public ProductSearchService(
        IEnumerable<IProductProvider> providers,
        IOpenAiService openAiService,
        IGpuPromptBuilder gpuPromptBuilder,
        IProductScorer productScorer
    )
    {
        _providers = providers;
        _gpuPromptBuilder = gpuPromptBuilder;
        _openAiService = openAiService;
        _productScorer = productScorer;
    }


    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var allResults = new List<ProductResult>();

        //fetch products from all providers
        foreach (var provider in _providers)
        {
            var results = await provider.SearchProductsAsync(request.Query);
            allResults.AddRange(results);
        }

        // evaluate each product list using gpt
        foreach (var product in allResults)
        {
            var prompt = _gpuPromptBuilder.BuildEvaluationPrompt(request, product);

            Console.WriteLine("PROMPT:");
            Console.WriteLine(prompt);

            var evaluation = await _openAiService.EvaluateGpuListingAsync(prompt);

            Console.WriteLine("GPT RESPONSE:");
            Console.WriteLine(
                $"Condition: {evaluation.Condition}, " +
                $"Score: {evaluation.ValueScore}, " +
                $"Reasoning: {evaluation.Reasoning}"
            );



            product.Score = _productScorer.CalculateScore(evaluation);

            product.Summary = evaluation.Reasoning;
            product.Condition = evaluation.Condition;
        }


        return allResults
            .OrderByDescending(x => x.Score)
            .ToList();
    }
}