namespace DealFinder.Api.Models;

public class RankingItemDto
{
    public string Category { get; set; } = "";
    public string CategoryLabel { get; set; } = "";
    public string ModelLabel { get; set; } = "";
    public int CatalogProductId { get; set; }
    public decimal MinPrice { get; set; }
    public int ListingCount { get; set; }
    public string TopListingUrl { get; set; } = "";
    public string TopListingImageUrl { get; set; } = "";
    public string TopListingMarketplace { get; set; } = "";

    // Cheapest listing from a non-risky (trusted) seller. Null when every listing
    // for this model is from a risky seller — the frontend hides those when
    // "Hide risky sellers" is on, instead of dropping the whole model.
    public decimal? TrustedMinPrice { get; set; }
    public string TrustedTopListingUrl { get; set; } = "";
    public string TrustedTopListingImageUrl { get; set; } = "";
    public string TrustedTopListingMarketplace { get; set; } = "";

    // Relative performance index for GPU/CPU models (null for other categories).
    public double? PerformanceScore { get; set; }

    // Performance points per pound — the value-ranking key. Null when no score exists.
    public double? ValueScore { get; set; }
}
