namespace DealFinder.Api.Services.Scoring;

public class QueryIntentParser : IQueryIntentParser
{
    private static readonly string[] PartsTerms =
    [
        "for parts",
        "parts",
        "repair",
        "spares",
        "faulty",
        "broken",
        "not working",
        "no display",
        "dead"
    ];

    private static readonly string[] MiningTerms =
    [
        "mining",
        "mined",
        "crypto",
        "rig"
    ];

    private static readonly string[] CheapTerms =
    [
        "cheap",
        "budget",
        "lowest price",
        "under",
        "deal"
    ];

    public SearchIntent Parse(string query)
    {
        var q = query.ToLowerInvariant();

        var wantsParts = PartsTerms.Any(term => q.Contains(term));

        return new SearchIntent
        {
            WantsPartsOrRepair = wantsParts,
            WantsWorkingGpu = !wantsParts,
            AcceptsMining = MiningTerms.Any(term => q.Contains(term)),
            WantsCheap = CheapTerms.Any(term => q.Contains(term))
        };
    }
}