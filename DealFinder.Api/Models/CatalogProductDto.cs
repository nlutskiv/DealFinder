namespace DealFinder.Api.Models;

public class CatalogProductDto
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public int ListingCount { get; set; }
}