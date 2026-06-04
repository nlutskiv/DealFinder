namespace DealFinder.Api.Models.OpenAI;

public class GpuEvaluationResult
{
    public bool Match { get; set; }

    public string Condition { get; set; } = "";

    public int RiskLevel { get; set; }

    public int ValueScore { get; set; }

    public string Summary { get; set; } = "";
}