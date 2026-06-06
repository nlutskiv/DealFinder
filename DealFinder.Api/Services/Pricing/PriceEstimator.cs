using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Pricing;

public class PriceEstimator : IPriceEstimator
{
    public decimal? EstimateMeanPrice(string query, List<ProductResult> products)
    {
        var normalizedQuery = Normalize(query);

        var matchingPrices = products
            .Where(product => IsTitleMatch(normalizedQuery, product.Title))
            .Select(product => product.Price + product.ShippingPrice)
            .Where(price => price > 0)
            .ToList();

        if (matchingPrices.Count == 0)
        {
            return null;
        }

        return matchingPrices.Average();
    }

    private static bool IsTitleMatch(string normalizedQuery, string title)
    {
        var normalizedTitle = Normalize(title);

        return normalizedTitle.Contains(normalizedQuery);
    }

    private static string Normalize(string value)
    {
        return value
            .ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ")
            .Replace("  ", " ")
            .Trim();
    }
}