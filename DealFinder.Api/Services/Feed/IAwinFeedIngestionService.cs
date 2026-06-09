namespace DealFinder.Api.Services.Feed;

public interface IAwinFeedIngestionService
{
    Task IngestAsync(
        Stream feedStream,
        string sourceName,
        CancellationToken ct = default
    );
}