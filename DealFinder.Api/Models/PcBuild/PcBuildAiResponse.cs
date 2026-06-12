using Newtonsoft.Json;

namespace DealFinder.Api.Models.PcBuild;

public class PcBuildAiResponse
{
    [JsonProperty("selections")]
    public Dictionary<string, int> Selections { get; set; } = new();

    [JsonProperty("totalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonProperty("compatibilityNotes")]
    public string CompatibilityNotes { get; set; } = "";

    [JsonProperty("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonProperty("summary")]
    public string Summary { get; set; } = "";

    [JsonProperty("isCompatible")]
    public bool IsCompatible { get; set; }
}
