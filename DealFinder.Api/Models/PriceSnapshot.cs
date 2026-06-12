namespace DealFinder.Api.Models;

public class PriceSnapshot
{
    public int Id { get; set; }
    public int CatalogProductId { get; set; }
    public decimal MinPrice { get; set; }
    public DateOnly RecordedDate { get; set; }
}
