namespace DealFinder.Api.Models.PcBuild;

public class PcBuildResult
{
    public decimal Budget { get; set; }
    public decimal TotalPrice { get; set; }
    public List<PcBuildPart> Parts { get; set; } = [];
    public string AiSummary { get; set; } = "";
    public List<string> CompatibilityWarnings { get; set; } = [];
    public bool IsCompatible { get; set; }
}
