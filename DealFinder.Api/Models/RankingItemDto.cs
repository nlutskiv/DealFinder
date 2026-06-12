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
}
