using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Scoring;

public static class ValueScorer
{
    public static double Calculate(
        ProductResult product,
        decimal? estimatedMeanPrice,
        double riskScore
    )
    {
        var totalPrice = product.Price + product.ShippingPrice;

        if (!estimatedMeanPrice.HasValue || estimatedMeanPrice <= 0 || totalPrice <= 0)
        {
            return riskScore >= 80 ? 10 : 50;
        }

        var ratio = (double)(totalPrice / estimatedMeanPrice.Value);

        var valueScore = ratio switch
        {
            < 0.50 => 70,  // suspiciously cheap, not max value
            < 0.75 => 95,  // great price
            < 0.90 => 85,  // good price
            < 1.05 => 70,  // fair price
            < 1.20 => 50,  // slightly expensive
            < 1.40 => 30,  // overpriced
            _ => 15        // very overpriced
        };

        if (riskScore >= 80)
        {
            valueScore = Math.Min(valueScore, 25);
        }
        else if (riskScore >= 60)
        {
            valueScore = Math.Min(valueScore, 45);
        }

        return valueScore;
    }
}