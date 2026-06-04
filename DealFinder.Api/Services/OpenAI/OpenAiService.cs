using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services;

public class OpenAiService : IOpenAiService
{
    public Task<GpuEvaluationResult> EvaluateGpuListingAsync(string prompt)
    {
        var result = new GpuEvaluationResult
        {
            Match = true,
            Condition = "good",
            RiskLevel = 10,
            ValueScore = 10,
            Summary = "shite"
        };

        return Task.FromResult(result);
    }
}