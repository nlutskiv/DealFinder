using System.Text;
using DealFinder.Api.Data;
using DealFinder.Api.Models;
using DealFinder.Api.Models.PcBuild;
using DealFinder.Api.Services.OpenAI;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DealFinder.Api.Services.PcBuild;

public class PcBuildService : IPcBuildService
{
    // Category -> (budget share, display label)
    private static readonly Dictionary<string, (decimal Share, string Label)> Categories = new()
    {
        ["gpu"]         = (0.35m, "GPU"),
        ["cpu"]         = (0.20m, "CPU"),
        ["motherboard"] = (0.12m, "Motherboard"),
        ["ram"]         = (0.10m, "RAM"),
        ["storage"]     = (0.08m, "Storage"),
        ["psu"]         = (0.07m, "PSU"),
        ["case"]        = (0.05m, "Case"),
        ["cooler"]      = (0.03m, "CPU Cooler"),
    };

    private const int CandidatesPerCategory = 5;
    private const decimal MinBudget = 150m;
    private const decimal MaxBudget = 20000m;

    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;
    private readonly IOpenAiService _openAiService;
    private readonly ILogger<PcBuildService> _logger;

    public PcBuildService(
        IDbContextFactory<DealFinderDbContext> dbFactory,
        IOpenAiService openAiService,
        ILogger<PcBuildService> logger
    )
    {
        _dbFactory = dbFactory;
        _openAiService = openAiService;
        _logger = logger;
    }

    public async Task<PcBuildResult> GenerateBuildAsync(PcBuildRequest request)
    {
        if (request.Budget < MinBudget || request.Budget > MaxBudget)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Budget),
                $"Budget must be between £{MinBudget} and £{MaxBudget}."
            );
        }

        var candidatesByCategory = await LoadCandidatesAsync(request.Budget);

        if (!candidatesByCategory.Any(kv => kv.Value.Count > 0))
        {
            return new PcBuildResult
            {
                Budget = request.Budget,
                AiSummary = "No PC components found in the database for this budget.",
                IsCompatible = false
            };
        }

        var aiResponse = await SelectCompatibleBuildAsync(request.Budget, candidatesByCategory);

        return BuildResult(request.Budget, candidatesByCategory, aiResponse);
    }

    private async Task<Dictionary<string, List<ProductListing>>> LoadCandidatesAsync(decimal budget)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var result = new Dictionary<string, List<ProductListing>>();

        foreach (var (category, (share, _)) in Categories)
        {
            var allocation = budget * share;
            // Allow up to 2x the allocation so the AI can reallocate budget between categories
            var maxPrice = allocation * 2.0m;

            var listings = await db.ProductListings
                .Where(p =>
                    p.Category.ToLower() == category &&
                    p.Price > 0 &&
                    p.Price <= maxPrice
                )
                .OrderBy(p => Math.Abs((double)(p.Price - allocation)))
                .Take(CandidatesPerCategory)
                .ToListAsync();

            result[category] = listings;

            _logger.LogInformation(
                "Loaded {Count} candidates for {Category} (allocation £{Allocation:F2}, max £{Max:F2})",
                listings.Count, category, allocation, maxPrice
            );
        }

        return result;
    }

    private async Task<PcBuildAiResponse> SelectCompatibleBuildAsync(
        decimal budget,
        Dictionary<string, List<ProductListing>> candidatesByCategory
    )
    {
        var prompt = BuildPrompt(budget, candidatesByCategory);

        _logger.LogInformation(
            "Sending PC build prompt to AI. Budget=£{Budget}, PromptLength={Length}",
            budget, prompt.Length
        );

        var json = await _openAiService.CompleteChatAsync(prompt, maxTokens: 800);

        _logger.LogDebug("PC build AI raw response: {Json}", json);

        var aiResponse = JsonConvert.DeserializeObject<PcBuildAiResponse>(json);

        if (aiResponse is null)
        {
            throw new InvalidOperationException("AI returned invalid PC build JSON.");
        }

        return aiResponse;
    }

    private static string BuildPrompt(
        decimal budget,
        Dictionary<string, List<ProductListing>> candidatesByCategory
    )
    {
        var sb = new StringBuilder();

        sb.AppendLine($"You are a PC building expert. Select one part per category to build a compatible PC within a £{budget:F2} total budget.");
        sb.AppendLine();
        sb.AppendLine("Available candidates (listing ID and price shown):");
        sb.AppendLine();

        foreach (var (category, (share, label)) in Categories)
        {
            var candidates = candidatesByCategory.GetValueOrDefault(category, []);
            var allocation = budget * share;

            sb.AppendLine($"## {label} (suggested allocation: £{allocation:F2})");

            if (candidates.Count == 0)
            {
                sb.AppendLine("  (no listings available — omit this category)");
            }
            else
            {
                foreach (var c in candidates)
                {
                    sb.AppendLine($"  [ID:{c.Id}] {c.Title} — £{c.Price:F2} (+£{c.ShippingPrice:F2} shipping) [{c.Condition}]");
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("Compatibility rules to enforce:");
        sb.AppendLine("- CPU socket MUST match Motherboard socket (Intel LGA1700/1851 ↔ Z790/Z890/B760 boards; AMD AM4/AM5 ↔ B550/X570/B650/X670 boards)");
        sb.AppendLine("- RAM type MUST match Motherboard (DDR4 or DDR5 — do NOT mix)");
        sb.AppendLine("- PSU wattage MUST cover GPU TDP + CPU TDP + 150 W overhead (rough guideline: RTX 4090 needs 850 W+, RTX 4070 needs 650 W+, budget GPUs need 500 W+)");
        sb.AppendLine("- If a category has no candidates, omit it from selections");
        sb.AppendLine();
        sb.AppendLine($"Budget constraint: total of selected parts (price + shipping) must be ≤ £{budget:F2}");
        sb.AppendLine();
        sb.AppendLine("Return JSON exactly in this format:");
        sb.AppendLine(@"{
  ""selections"": { ""gpu"": <listingId>, ""cpu"": <listingId>, ""motherboard"": <listingId>, ""ram"": <listingId>, ""storage"": <listingId>, ""psu"": <listingId>, ""case"": <listingId>, ""cooler"": <listingId> },
  ""totalPrice"": <sum of selected part prices incl. shipping>,
  ""compatibilityNotes"": ""<brief explanation of why this build is compatible>"",
  ""warnings"": [""<any concern>""],
  ""summary"": ""<2-3 sentences on the build's performance level and value>"",
  ""isCompatible"": true
}");

        return sb.ToString();
    }

    private static PcBuildResult BuildResult(
        decimal budget,
        Dictionary<string, List<ProductListing>> candidatesByCategory,
        PcBuildAiResponse aiResponse
    )
    {
        var allListings = candidatesByCategory.Values
            .SelectMany(l => l)
            .ToDictionary(l => l.Id);

        var parts = new List<PcBuildPart>();

        foreach (var (category, listingId) in aiResponse.Selections)
        {
            if (!allListings.TryGetValue(listingId, out var listing))
            {
                continue;
            }

            var label = Categories.TryGetValue(category, out var info) ? info.Label : category;

            parts.Add(new PcBuildPart
            {
                Category = category,
                CategoryLabel = label,
                ListingId = listing.Id,
                Title = listing.Title,
                Price = listing.Price,
                ShippingPrice = listing.ShippingPrice,
                Url = listing.Url,
                ImageUrl = listing.ImageUrl,
                Marketplace = listing.Marketplace,
                Condition = listing.Condition,
            });
        }

        return new PcBuildResult
        {
            Budget = budget,
            TotalPrice = aiResponse.TotalPrice > 0
                ? aiResponse.TotalPrice
                : parts.Sum(p => p.Price + p.ShippingPrice),
            Parts = parts,
            AiSummary = string.IsNullOrWhiteSpace(aiResponse.Summary)
                ? aiResponse.CompatibilityNotes
                : aiResponse.Summary,
            CompatibilityWarnings = aiResponse.Warnings ?? [],
            IsCompatible = aiResponse.IsCompatible
        };
    }

}
