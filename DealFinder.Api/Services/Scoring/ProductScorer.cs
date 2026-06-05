using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.Scoring;


public class ProductScorer : IProductScorer
{
    public double CalculateScore(GpuEvaluationResult evaluation)
    {
        return 0.65 * evaluation.ValueScore
            + 0.25 * evaluation.QueryMatchScore
            - 0.10 * evaluation.RiskLevel;
    }
}