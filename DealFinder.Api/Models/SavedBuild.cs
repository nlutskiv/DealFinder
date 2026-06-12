namespace DealFinder.Api.Models;

public class SavedBuild
{
    public string Id { get; set; } = "";
    public decimal Budget { get; set; }
    public decimal TotalPrice { get; set; }
    public string PartsJson { get; set; } = "";
    public string AiSummary { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
