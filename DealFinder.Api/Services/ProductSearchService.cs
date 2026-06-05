using DealFinder.Api.Models;
using DealFinder.Api.Models.OpenAI;
using DealFinder.Api.Providers;
using DealFinder.Api.Services.OpenAI;

namespace DealFinder.Api.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly IEnumerable<IProductProvider> _providers;
    private readonly IOpenAiService _openAiService;
    private readonly IGpuPromptBuilder _gpuPromptBuilder;


    public ProductSearchService(
        IEnumerable<IProductProvider> providers,
        IOpenAiService openAiService,
        IGpuPromptBuilder gpuPromptBuilder
    )
    {
        _providers = providers;
        _gpuPromptBuilder = gpuPromptBuilder;
        _openAiService = openAiService;
    }


    public async Task<List<ProductResult>> SearchAsync(ProductSearchRequest request)
    {
        var allResults = new List<ProductResult>();

        foreach (var provider in _providers)
        {
            var results = await provider.SearchProductsAsync(request.Query);
            allResults.AddRange(results);
        }

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



            product.Score =
                0.65 * evaluation.ValueScore
                + 0.25 * evaluation.QueryMatchScore
                - 0.10 * evaluation.RiskLevel;
                
            product.Summary = evaluation.Reasoning;
            product.Condition = evaluation.Condition;
        }


        return allResults
            .OrderByDescending(x => x.Score)
            .ToList();
    }
}