using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Pricing;

public interface IPriceEstimator
{
    decimal? EstimateMeanPrice(string query, List<ProductResult> products);
}