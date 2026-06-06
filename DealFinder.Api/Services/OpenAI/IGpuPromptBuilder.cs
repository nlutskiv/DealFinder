using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public interface IGpuPromptBuilder
{
    string BuildEvaluationPrompt(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    );
}