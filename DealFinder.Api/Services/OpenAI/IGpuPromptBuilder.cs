using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public interface IGpuPromptBuilder
{
    string BuildEvaluationPrompt(ProductResult product);
}