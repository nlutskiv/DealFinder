using DealFinder.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.Catalog;

/// <summary>
/// Applies a static relative-performance index to GPU/CPU catalog entries by label match.
/// Scores are approximate aggregate benchmark figures normalised to ~100 for the fastest
/// current consumer part, and only need to be directionally right for value ranking.
/// </summary>
public static class PerformanceScoreSeeder
{
    // Longest keys must win ("RTX 4070 Ti Super" before "RTX 4070").
    private static readonly Dictionary<string, double> GpuScores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RTX 5090"] = 100, ["RTX 4090"] = 86, ["RTX 5080"] = 78,
        ["RTX 4080 Super"] = 70, ["RTX 4080"] = 68,
        ["RTX 5070 Ti"] = 66, ["RX 7900 XTX"] = 65,
        ["RTX 4070 Ti Super"] = 61, ["RX 7900 XT"] = 58,
        ["RTX 5070"] = 56, ["RTX 4070 Ti"] = 55, ["RTX 4070 Super"] = 53,
        ["RX 7900 GRE"] = 49, ["RX 7800 XT"] = 47, ["RTX 4070"] = 46,
        ["RTX 5060 Ti"] = 42, ["RX 7700 XT"] = 40,
        ["RTX 4060 Ti"] = 36, ["RTX 5060"] = 36,
        ["Arc B580"] = 33, ["RX 7600 XT"] = 31, ["RTX 4060"] = 30,
        ["RX 7600"] = 29, ["Arc B570"] = 28,
        ["RTX 3060 Ti"] = 28, ["RTX 3060"] = 26, ["RX 6650 XT"] = 25,
        ["RX 6600"] = 24, ["RTX 3050"] = 18, ["GTX 1660 Super"] = 16,
        ["RX 6500 XT"] = 13, ["GTX 1650"] = 11,
        ["GT 1030"] = 4, ["GT 730"] = 1.5, ["GT 710"] = 1,
    };

    private static readonly Dictionary<string, double> CpuScores = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Ryzen 9 9950X3D"] = 100, ["Ryzen 9 9950X"] = 98,
        ["i9-14900K"] = 95, ["Ryzen 9 7950X3D"] = 92, ["Ryzen 9 7950X"] = 91,
        ["i9-13900K"] = 89, ["Ryzen 9 9900X3D"] = 84, ["Ryzen 9 9900X"] = 82,
        ["i7-14700K"] = 84, ["Ryzen 9 7900X"] = 78, ["Ryzen 9 7900"] = 74,
        ["i7-13700K"] = 76, ["Ryzen 7 9800X3D"] = 62,
        ["i5-14600K"] = 60, ["Ryzen 7 9700X"] = 55, ["i5-13600K"] = 57,
        ["Ryzen 7 7700X"] = 53, ["Ryzen 7 7800X3D"] = 50, ["Ryzen 7 7700"] = 50,
        ["Ryzen 5 9600X"] = 44, ["Ryzen 5 7600X"] = 42, ["i5-14400F"] = 41,
        ["Ryzen 5 7600"] = 40, ["i5-13400F"] = 38, ["i5-12400F"] = 33,
        ["Ryzen 7 5800X3D"] = 35, ["Ryzen 7 5700X"] = 33,
        ["Ryzen 5 5600X"] = 30, ["Ryzen 5 5600"] = 29, ["Ryzen 5 5500"] = 26,
        ["i3-14100"] = 25, ["i3-12100F"] = 22, ["Ryzen 5 4500"] = 20,
    };

    public static async Task ApplyAsync(DealFinderDbContext db)
    {
        var products = await db.ProductCatalog
            .Where(p => p.Category.ToLower() == "gpu" || p.Category.ToLower() == "cpu")
            .ToListAsync();

        foreach (var product in products)
        {
            var table = product.Category.Equals("gpu", StringComparison.OrdinalIgnoreCase)
                ? GpuScores
                : CpuScores;

            var match = table
                .Where(kv => product.Label.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(kv => kv.Key.Length)
                .Select(kv => (double?)kv.Value)
                .FirstOrDefault();

            if (match.HasValue && product.PerformanceScore != match)
            {
                product.PerformanceScore = match;
            }
        }

        await db.SaveChangesAsync();
    }
}
