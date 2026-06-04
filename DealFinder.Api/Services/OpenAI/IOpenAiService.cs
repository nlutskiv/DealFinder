using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services;

public interface IOpenAiService
{
    Task<GpuEvaluationResult> EvaluateGpuListingAsync(string prompt);
}