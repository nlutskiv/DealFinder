using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.Scoring;

public interface IProductScorer
{
    double CalculateScore(ListingEvaluationResult evaluation);
}