using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.OpenAI;

public interface IOpenAiService
{
    Task<ListingEvaluationResult> EvaluateListingAsync(string prompt);
}