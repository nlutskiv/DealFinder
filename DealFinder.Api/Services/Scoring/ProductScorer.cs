using DealFinder.Api.Models.OpenAI;

namespace DealFinder.Api.Services.Scoring;

public class ProductScorer : IProductScorer
{
    public double CalculateScore(ListingEvaluationResult evaluation)
    {
        var positiveScore =
            0.35 * evaluation.QueryMatchScore +
            0.30 * evaluation.ValueScore +
            0.20 * evaluation.ConditionScore +
            0.15 * evaluation.SellerTrustScore;

        var riskPenalty =
            0.30 * evaluation.RiskScore;

        var score = positiveScore - riskPenalty;

        return Math.Clamp(score, 0, 100);
    }
}