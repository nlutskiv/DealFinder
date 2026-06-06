using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Scoring;

public interface IPreFilterScorer
{
    double Score(
        string query,
        ProductResult product,
        decimal? meanPrice,
        SearchIntent intent
    );
}