using DealFinder.Api.Models;

namespace DealFinder.Api.Services.Feed;

public interface IAwinFeedIngestionService
{
    Task IngestAsync(
        Stream feedStream,
        string sourceName,
        Dictionary<(string Category, string Label), ProductCatalog> catalogCache,
        Dictionary<string, ProductListing> listingCache,
        CancellationToken ct = default
    );
}
