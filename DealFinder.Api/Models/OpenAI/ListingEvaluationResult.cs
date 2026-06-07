using Newtonsoft.Json;

namespace DealFinder.Api.Models.OpenAI;

public class ListingEvaluationResult
{
    [JsonProperty("model")]
    public string Model { get; set; } = "unknown";

    [JsonProperty("vram_gb")]
    public int? VRamGb { get; set; }

    [JsonProperty("query_match_score")]
    public double QueryMatchScore { get; set; }

    [JsonProperty("condition")]
    public string Condition { get; set; } = "unknown";

    [JsonProperty("condition_score")]
    public double ConditionScore { get; set; }

    [JsonProperty("seller_trust_score")]
    public double SellerTrustScore { get; set; }

    [JsonProperty("risk_score")]
    public double RiskScore { get; set; }

    [JsonProperty("risk_label")]
    public string RiskLabel { get; set; } = "MEDIUM";

    [JsonProperty("value_score")]
    public double ValueScore { get; set; }

    [JsonProperty("detected_issues")]
    public List<string> DetectedIssues { get; set; } = new();

    [JsonProperty("positive_signals")]
    public List<string> PositiveSignals { get; set; } = new();

    [JsonProperty("reasoning")]
    public string Reasoning { get; set; } = "";

    [JsonProperty("estimated_used_price_low")]
    public decimal? EstimatedUsedPriceLow { get; set; }

    [JsonProperty("estimated_used_price_typical")]
    public decimal? EstimatedUsedPriceTypical { get; set; }

    [JsonProperty("estimated_used_price_high")]
    public decimal? EstimatedUsedPriceHigh { get; set; }
}