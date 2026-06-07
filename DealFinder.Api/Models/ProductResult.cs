namespace DealFinder.Api.Models;

public class ProductResult
{
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
    public double Score { get; set; }
    public string Summary { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Category { get; set; } = "";
}