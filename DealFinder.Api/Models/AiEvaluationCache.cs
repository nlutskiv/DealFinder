namespace DealFinder.Api.Models;

public class AiEvaluationCache
{
    public int Id { get; set; }

    public string Url { get; set; } = "";
    public string UrlHash { get; set; } = "";

    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public decimal ShippingPrice { get; set; }

    public string Condition { get; set; } = "";
    public double ConditionScore { get; set; }
    public double SellerTrustScore { get; set; }
    public double RiskScore { get; set; }
    public string RiskLabel { get; set; } = "MEDIUM";

    public string DetectedIssuesJson { get; set; } = "[]";
    public string PositiveSignalsJson { get; set; } = "[]";

    public string AnalysisSummary { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}