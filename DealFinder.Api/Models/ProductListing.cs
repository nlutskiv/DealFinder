namespace DealFinder.Api.Models;

public class ProductListing
{
    public int Id { get; set; }

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    public decimal Price { get; set; }
    public decimal ShippingPrice { get; set; }

    public string Condition { get; set; } = "";
    public string SellerName { get; set; } = "";
    public double? SellerFeedbackPercentage { get; set; }
    public int? SellerFeedbackScore { get; set; }

    public string Marketplace { get; set; } = "";
    public string Url { get; set; } = "";
    public string ImageUrl { get; set; } = "";

    public int? ProductCatalogId { get; set; }
    public string Category { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}