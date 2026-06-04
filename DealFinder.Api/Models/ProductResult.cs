namespace DealFinder.Api.Models;

public class ProductResult
{
    public string Title { get; set; } = "";

    public decimal Price { get; set; }

    public string Marketplace { get; set; } = "";

    public string Url { get; set; } = "";

    public double Score { get; set; }
}