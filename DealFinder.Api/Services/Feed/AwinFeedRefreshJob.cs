using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using DealFinder.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.Feed;

public class AwinFeedRefreshJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AwinFeedRefreshJob> _logger;

    public AwinFeedRefreshJob(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AwinFeedRefreshJob> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var runOnStartup = _config.GetValue("Awin:RunOnStartup", false);
        var syncIntervalHours = _config.GetValue("Awin:SyncIntervalHours", 12);

        if (runOnStartup)
        {
            await RunIngestionAsync(ct);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(syncIntervalHours));

        while (await timer.WaitForNextTickAsync(ct))
        {
            await RunIngestionAsync(ct);
        }
    }

    private async Task RunIngestionAsync(CancellationToken ct = default)
    {
        try
        {
            var feeds = await GetAllowedFeedsAsync(ct);

            _logger.LogInformation(
                "Found {Count} Awin feed(s) to ingest: {Feeds}",
                feeds.Count,
                string.Join(", ", feeds.Select(feed => $"{feed.AdvertiserId}:{feed.AdvertiserName}"))
            );

            var maxConcurrentFeeds = _config.GetValue("Awin:MaxConcurrentFeeds", 2);

            using var semaphore = new SemaphoreSlim(maxConcurrentFeeds);

            var tasks = feeds.Select(async feed =>
            {
                await semaphore.WaitAsync(ct);

                try
                {
                    await IngestSingleFeedSafelyAsync(feed, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            await PruneStaleListingsAsync(ct);

            _logger.LogInformation("Awin feed refresh finished.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Awin feed refresh failed.");
        }
    }

    // Listings are upserted by URL and never re-seen once a retailer drops them from
    // their feed (sold out, delisted, or now rejected by classification). Remove anything
    // not seen for 48h — two full sync cycles — so one failed sync can't wipe live data.
    private async Task PruneStaleListingsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<DealFinderDbContext>>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var cutoff = DateTime.UtcNow.AddHours(-48);

        var removed = await db.ProductListings
            .Where(l => l.LastSeenUtc < cutoff)
            .ExecuteDeleteAsync(ct);

        if (removed > 0)
        {
            _logger.LogInformation("Pruned {Count} stale listing(s) not seen since {Cutoff:u}.", removed, cutoff);
        }
    }

    private async Task<List<AwinFeedEntry>> GetAllowedFeedsAsync(CancellationToken ct)
    {
        var feedListUrl = _config["Awin:FeedListUrl"]
            ?? throw new InvalidOperationException("Awin:FeedListUrl not configured.");

        var allowedAdvertiserIds = ReadConfigSet("Awin:AllowedAdvertiserIds");
        var allowedAdvertiserNames = ReadConfigSet("Awin:AllowedAdvertiserNames");

        var http = _httpClientFactory.CreateClient("AwinFeeds");

        _logger.LogInformation("Downloading Awin feed list from {FeedListUrl}", feedListUrl);

        var feedListText = await http.GetStringAsync(feedListUrl, ct);

        var feeds = ExtractFeedEntries(feedListText)
            .Where(feed => IsAllowedFeed(feed, allowedAdvertiserIds, allowedAdvertiserNames))
            .ToList();

        var advertiserOrder = (_config["Awin:AllowedAdvertiserIds"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index, StringComparer.OrdinalIgnoreCase);

        return feeds
            .OrderBy(feed =>
                advertiserOrder.TryGetValue(feed.AdvertiserId, out var index)
                    ? index
                    : int.MaxValue
            )
            .ToList();
    }

    private async Task IngestSingleFeedSafelyAsync(
        AwinFeedEntry feed,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Starting Awin feed. AdvertiserId={AdvertiserId}, AdvertiserName={AdvertiserName}",
                feed.AdvertiserId,
                feed.AdvertiserName
            );

            _logger.LogInformation(
                "Downloading Awin feed. AdvertiserId={AdvertiserId}, AdvertiserName={AdvertiserName}, Url={Url}",
                feed.AdvertiserId,
                feed.AdvertiserName,
                feed.Url
            );

            var http = _httpClientFactory.CreateClient("AwinFeeds");

            using var scope = _scopeFactory.CreateScope();

            var ingestion = scope.ServiceProvider
                .GetRequiredService<IAwinFeedIngestionService>();

            if (!IsAllowedFeedUrl(feed.Url))
            {
                _logger.LogWarning(
                    "Skipping feed with disallowed URL scheme or host. AdvertiserId={AdvertiserId}, Url={Url}",
                    feed.AdvertiserId, feed.Url
                );
                return;
            }

            await using var stream = await http.GetStreamAsync(feed.Url, ct);
            await using var decompressed = WrapIfCompressed(stream, feed.Url);

            await ingestion.IngestAsync(
                feedStream: decompressed,
                sourceName: string.IsNullOrWhiteSpace(feed.AdvertiserName)
                    ? $"awin_{feed.AdvertiserId}"
                    : $"awin_{Slug(feed.AdvertiserName)}",
                ct: ct
            );

            _logger.LogInformation(
                "Finished Awin feed. AdvertiserId={AdvertiserId}, AdvertiserName={AdvertiserName}",
                feed.AdvertiserId,
                feed.AdvertiserName
            );
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Awin feed failed. AdvertiserId={AdvertiserId}, AdvertiserName={AdvertiserName}",
                feed.AdvertiserId,
                feed.AdvertiserName
            );
        }
    }

    private HashSet<string> ReadConfigSet(string key)
    {
        var raw = _config[key];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsAllowedFeed(
        AwinFeedEntry feed,
        HashSet<string> allowedAdvertiserIds,
        HashSet<string> allowedAdvertiserNames)
    {
        // Safer: import nothing unless explicitly allowlisted.
        if (allowedAdvertiserIds.Count == 0 && allowedAdvertiserNames.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(feed.AdvertiserId) &&
            allowedAdvertiserIds.Contains(feed.AdvertiserId))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(feed.AdvertiserName) &&
            allowedAdvertiserNames.Contains(feed.AdvertiserName))
        {
            return true;
        }

        return false;
    }

    private static List<AwinFeedEntry> ExtractFeedEntries(string text)
    {
        text = text.Trim();

        if (text.StartsWith("{") || text.StartsWith("["))
        {
            var jsonFeeds = TryExtractFromJson(text);

            if (jsonFeeds.Count > 0)
            {
                return jsonFeeds;
            }
        }

        var delimitedFeeds = TryExtractFromDelimitedText(text);

        if (delimitedFeeds.Count > 0)
        {
            return delimitedFeeds;
        }

        return Regex.Matches(text, @"https?://[^\s""'<>,]+", RegexOptions.IgnoreCase)
            .Select(match => match.Value.Trim())
            .Where(url => !url.Contains("feedlist", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(url => new AwinFeedEntry
            {
                Url = url
            })
            .ToList();
    }

    private static List<AwinFeedEntry> TryExtractFromJson(string text)
    {
        var feeds = new List<AwinFeedEntry>();

        try
        {
            using var doc = JsonDocument.Parse(text);
            WalkJson(doc.RootElement, feeds);
        }
        catch
        {
            return [];
        }

        return feeds
            .Where(feed => !string.IsNullOrWhiteSpace(feed.Url))
            .DistinctBy(feed => feed.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void WalkJson(JsonElement element, List<AwinFeedEntry> feeds)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var possibleUrl =
                GetJsonString(element, "url") ??
                GetJsonString(element, "downloadUrl") ??
                GetJsonString(element, "download_url") ??
                GetJsonString(element, "feedUrl") ??
                GetJsonString(element, "feed_url");

            if (!string.IsNullOrWhiteSpace(possibleUrl) &&
                possibleUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                feeds.Add(new AwinFeedEntry
                {
                    Url = possibleUrl,
                    AdvertiserId =
                        GetJsonString(element, "id") ??
                        GetJsonString(element, "advertiserId") ??
                        GetJsonString(element, "advertiser_id") ??
                        GetJsonString(element, "merchant_id") ??
                        GetJsonString(element, "merchantId") ??
                        "",
                    AdvertiserName =
                        GetJsonString(element, "advertiser") ??
                        GetJsonString(element, "advertiserName") ??
                        GetJsonString(element, "advertiser_name") ??
                        GetJsonString(element, "merchant_name") ??
                        GetJsonString(element, "merchantName") ??
                        ""
                });
            }

            foreach (var property in element.EnumerateObject())
            {
                WalkJson(property.Value, feeds);
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                WalkJson(item, feeds);
            }
        }
    }

    private static string? GetJsonString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.ToString(),
            _ => null
        };
    }

    private static List<AwinFeedEntry> TryExtractFromDelimitedText(string text)
    {
        var feeds = new List<AwinFeedEntry>();

        var firstLine = text.Split('\n').FirstOrDefault() ?? "";

        var delimiter =
            firstLine.Contains('\t') ? '\t' :
            firstLine.Contains('|') ? '|' :
            firstLine.Contains(';') ? ';' :
            ',';

        var lines = text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (lines.Count < 2)
        {
            return feeds;
        }

        var headers = SplitLine(lines[0], delimiter)
            .Select(NormaliseHeader)
            .ToList();

        var urlIndex = FindFirstHeaderIndex(
            headers,
            "url",
            "downloadurl",
            "feedurl"
        );

        var advertiserIdIndex = FindFirstHeaderIndex(
            headers,
            "id",
            "advertiserid",
            "merchantid"
        );

        var advertiserNameIndex = FindFirstHeaderIndex(
            headers,
            "advertiser",
            "advertisername",
            "merchantname"
        );

        if (urlIndex < 0)
        {
            return feeds;
        }

        foreach (var line in lines.Skip(1))
        {
            var parts = SplitLine(line, delimiter);

            if (urlIndex >= parts.Count)
            {
                continue;
            }

            var url = parts[urlIndex].Trim();

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            feeds.Add(new AwinFeedEntry
            {
                Url = url,
                AdvertiserId = advertiserIdIndex >= 0 && advertiserIdIndex < parts.Count
                    ? parts[advertiserIdIndex].Trim()
                    : "",
                AdvertiserName = advertiserNameIndex >= 0 && advertiserNameIndex < parts.Count
                    ? parts[advertiserNameIndex].Trim()
                    : ""
            });
        }

        return feeds
            .Where(feed => !feed.Url.Contains("feedlist", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(feed => feed.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> SplitLine(string line, char delimiter)
    {
        return line.Split(delimiter)
            .Select(part => part.Trim().Trim('"'))
            .ToList();
    }

    private static int FindFirstHeaderIndex(List<string> headers, params string[] names)
    {
        foreach (var name in names)
        {
            var normalisedName = NormaliseHeader(name);

            var index = headers.FindIndex(header =>
                header.Equals(normalisedName, StringComparison.OrdinalIgnoreCase)
            );

            if (index >= 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static string NormaliseHeader(string value)
    {
        return value
            .Trim()
            .Trim('"')
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "")
            .ToLowerInvariant();
    }

    private static Stream WrapIfCompressed(Stream stream, string url)
    {
        if (
            url.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("compression/gzip", StringComparison.OrdinalIgnoreCase)
        )
        {
            return new GZipStream(stream, CompressionMode.Decompress);
        }

        return stream;
    }

    private static bool IsAllowedFeedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
        var host = uri.Host.ToLowerInvariant();
        // Block requests to private/loopback ranges and cloud metadata endpoints.
        if (host == "localhost" || host == "127.0.0.1" || host == "::1") return false;
        if (host.StartsWith("169.254.") || host.StartsWith("10.") || host.StartsWith("192.168.")) return false;
        if (host == "metadata.google.internal" || host.EndsWith(".internal")) return false;
        return true;
    }

    private static string Slug(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var slug = Regex.Replace(lower, @"[^a-z0-9]+", "_").Trim('_');

        return string.IsNullOrWhiteSpace(slug)
            ? "unknown"
            : slug;
    }

    private sealed class AwinFeedEntry
    {
        public string Url { get; set; } = "";
        public string AdvertiserId { get; set; } = "";
        public string AdvertiserName { get; set; } = "";
    }
}