namespace DealFinder.Api.Models;

public class ProductSearchRequest
{
    public int CatalogProductId { get; set; }

    // Internal only for now. Frontend should not rely on this.
    // ProductSearchService sets this after loading the catalog product.
    public string Query { get; set; } = "";
}