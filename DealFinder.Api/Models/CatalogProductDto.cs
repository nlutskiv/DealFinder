namespace DealFinder.Api.Models;

public class CatalogProductDto
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public int ListingCount { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
}