namespace DealFinder.Api.Models;

public class ProductCatalog
{
    public int Id { get; set; }

    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public string SearchQuery { get; set; } = "";
    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;

    // Relative performance index (higher = faster), used for value-per-pound ranking.
    // Populated from a static benchmark table for GPU/CPU models; null for other categories.
    public double? PerformanceScore { get; set; }
}