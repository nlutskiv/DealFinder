using Newtonsoft.Json;

namespace DealFinder.Api.Models.OpenAI;

public class GpuEvaluationResult
{
    [JsonProperty("model")]
    public string Model { get; set; } = "";

    [JsonProperty("vram_gb")]
    public int? VRamGb { get; set; }

    [JsonProperty("query_match_score")]
    public int QueryMatchScore { get; set; }

    [JsonProperty("condition")]
    public string Condition { get; set; } = "";

    [JsonProperty("risk_level")]
    public int RiskLevel { get; set; }

    [JsonProperty("value_score")]
    public int ValueScore { get; set; }

    [JsonProperty("reasoning")]
    public string Reasoning { get; set; } = "";
}