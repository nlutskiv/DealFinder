namespace DealFinder.Api.Models;

public class ProductCatalog
{
    public int Id { get; set; }

    public string Category { get; set; } = "";
    public string Label { get; set; } = "";
    public string SearchQuery { get; set; } = "";
    public string Description { get; set; } = "";

    public bool IsActive { get; set; } = true;
}