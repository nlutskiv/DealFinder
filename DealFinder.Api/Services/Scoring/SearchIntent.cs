namespace DealFinder.Api.Services.Scoring;

public class SearchIntent
{
    public bool WantsWorkingGpu { get; set; } = true;
    public bool WantsPartsOrRepair { get; set; } = false;
    public bool AcceptsMining { get; set; } = false;
    public bool WantsCheap { get; set; } = false;
}