using DealFinder.Api.Models;
using DealFinder.Api.Services.Classification;

namespace DealFinder.Api.Services.OpenAI;

public class CategoryPromptBuilder : IListingPromptBuilder
{
    private readonly GpuPromptBuilder _gpuPromptBuilder;
    private readonly CpuPromptBuilder _cpuPromptBuilder;

    public CategoryPromptBuilder()
    {
        _gpuPromptBuilder = new GpuPromptBuilder();
        _cpuPromptBuilder = new CpuPromptBuilder();
    }

    public string BuildEvaluationPrompt(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    )
    {
        return product.Category switch
        {
            ProductCategory.Gpu => _gpuPromptBuilder.BuildEvaluationPrompt(request, product, estimatedMeanPrice),
            ProductCategory.Cpu => _cpuPromptBuilder.BuildEvaluationPrompt(request, product, estimatedMeanPrice),
            _ => GenericPromptBuilder.BuildEvaluationPrompt(request, product, estimatedMeanPrice)
        };
    }
}