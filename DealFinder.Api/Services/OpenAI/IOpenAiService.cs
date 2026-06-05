using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.OpenAI;

public interface IOpenAiService
{
    Task<GpuEvaluationResult> EvaluateGpuListingAsync(string prompt);
}