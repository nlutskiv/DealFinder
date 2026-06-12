namespace DealFinder.Api.Models;

public class PriceAlert
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public int CatalogProductId { get; set; }
    public decimal TargetPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? TriggeredAtUtc { get; set; }
}
