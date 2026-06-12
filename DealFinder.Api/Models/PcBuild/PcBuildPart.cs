namespace DealFinder.Api.Models.PcBuild;

public class PcBuildPart
{
    public string Category { get; set; } = "";
    public string CategoryLabel { get; set; } = "";
    public int ListingId { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal ShippingPrice { get; set; }
    public string Url { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public string Condition { get; set; } = "";
}
