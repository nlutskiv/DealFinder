namespace DealFinder.Api.Models;

public class AiEvaluationCache
{
    public int Id { get; set; }

    public string Url { get; set; } = "";
    public string UrlHash { get; set; } = "";

    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal ShippingPrice { get; set; }

    public double Score { get; set; }
    public string Summary { get; set; } = "";
    public string Condition { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}