using DealFinder.Api.Models;
using System.Text.RegularExpressions;

namespace DealFinder.Api.Services.Scoring;

public class PreFilterScorer : IPreFilterScorer
{
    private static readonly string[] HardKills =
    [
        "spares", "repair", "for parts", "not working", "faulty", "broken",
        "no display", "no signal", "dead", "damaged", "bent pins", "water damage",
        "bios fault", "won't post", "will not post"
    ];

    private static readonly string[] SoftPenalties =
    [
        "untested", "sold as seen", "no returns", "as is", "unknown fault",
        "mining", "crypto", "ethereum", "rig", "farm",
        "noisy fan", "fan noise", "coil whine", "artifacting", "artifacts",
        "overheating", "thermal", "crashing", "crashes", "blue screen", "bsod",
        "missing", "no box", "scratched", "marks", "worn"
    ];

    private static readonly string[] PositiveSignals =
    [
        "tested", "working", "fully working", "perfect", "excellent",
        "benchmark", "3dmark", "furmark", "stress test", "warranty",
        "receipt", "boxed", "original box", "barely used", "light use",
        "mint", "immaculate", "clean", "no issues", "runs well"
    ];

    public double Score(
        string query,
        ProductResult product,
        decimal? meanPrice,
        SearchIntent intent
    )
    {
        var title = product.Title.ToLowerInvariant();
        var desc = product.Description.ToLowerInvariant();
        var combined = $"{title} {desc}";
        var totalPrice = product.Price + product.ShippingPrice;

        var hardKillHits = HardKills.Count(term => combined.Contains(term));

        if (intent.WantsPartsOrRepair)
        {
            return ScorePartsSearch(
                query,
                title,
                combined,
                totalPrice,
                meanPrice,
                product,
                hardKillHits
            );
        }

        return ScoreWorkingGpuSearch(
            query,
            title,
            combined,
            totalPrice,
            meanPrice,
            product,
            intent,
            hardKillHits
        );
    }

    private static double ScoreWorkingGpuSearch(
        string query,
        string title,
        string combined,
        decimal totalPrice,
        decimal? meanPrice,
        ProductResult product,
        SearchIntent intent,
        int hardKillHits
    )
    {
        // Normal buyers should not see broken/repair cards ranked highly.
        if (hardKillHits > 0)
        {
            return 5.0;
        }

        double score = 50.0;

        score += QueryMatchScore(query, title);
        score += WorkingPriceScore(totalPrice, meanPrice);
        score += WorkingConditionScore(combined);
        score += SellerScore(product);
        score -= SoftPenaltyScore(combined, intent);

        return Math.Clamp(score, 0, 100);
    }

    private static double ScorePartsSearch(
        string query,
        string title,
        string combined,
        decimal totalPrice,
        decimal? meanPrice,
        ProductResult product,
        int hardKillHits
    )
    {
        double score = 0.0;

        // For parts search, model match matters most.
        score += QueryMatchScore(query, title) * 1.6;

        // Broken/faulty wording is desirable here.
        if (hardKillHits > 0)
        {
            score += 25;
        }
        else
        {
            // Working cards are usually not what "for parts" buyers want.
            score -= 20;
        }

        // For parts, cheapness matters a lot.
        score += PartsPriceScore(totalPrice, meanPrice);

        // Seller trust matters, but less than price/model/fault status.
        score += SellerScore(product) * 0.5;

        // Penalize listings that look too premium/working for a parts search.
        var positiveHits = PositiveSignals.Count(term => combined.Contains(term));
        if (positiveHits >= 2 && hardKillHits == 0)
        {
            score -= 10;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static double QueryMatchScore(string query, string title)
    {
        var q = RemoveIntentWords(query.ToLowerInvariant().Trim());

        if (string.IsNullOrWhiteSpace(q))
        {
            return 0;
        }

        if (title.Contains(q))
        {
            return 25;
        }

        var queryNumbers = Regex.Matches(q, @"\d{3,4}")
            .Select(match => match.Value)
            .ToList();

        if (!queryNumbers.Any())
        {
            return 0;
        }

        var numberMatch = queryNumbers.All(title.Contains);

        if (!numberMatch)
        {
            return -15;
        }

        var querySuffix = ExtractSuffix(q);
        var titleSuffix = ExtractSuffix(title);

        if (querySuffix != null && titleSuffix != null && querySuffix != titleSuffix)
        {
            return 5;
        }

        if (querySuffix != null && titleSuffix == null)
        {
            return 8;
        }

        return 18;
    }

    private static double WorkingPriceScore(decimal totalPrice, decimal? meanPrice)
    {
        if (!meanPrice.HasValue || meanPrice <= 0 || totalPrice <= 0)
        {
            return 0;
        }

        var ratio = (double)(totalPrice / meanPrice.Value);

        return ratio switch
        {
            < 0.50 => -10, // suspiciously cheap
            < 0.75 => 20,
            < 0.90 => 15,
            < 1.05 => 10,
            < 1.20 => 3,
            < 1.40 => -5,
            _ => -15
        };
    }

    private static double PartsPriceScore(decimal totalPrice, decimal? meanPrice)
    {
        if (totalPrice <= 0)
        {
            return 0;
        }

        if (!meanPrice.HasValue || meanPrice <= 0)
        {
            return totalPrice switch
            {
                <= 75 => 45,
                <= 100 => 40,
                <= 150 => 30,
                <= 200 => 20,
                <= 300 => 5,
                _ => -15
            };
        }

        var ratio = (double)(totalPrice / meanPrice.Value);

        return ratio switch
        {
            < 0.20 => 45,
            < 0.35 => 40,
            < 0.50 => 30,
            < 0.70 => 15,
            < 1.00 => 0,
            _ => -20
        };
    }

    private static double WorkingConditionScore(string combined)
    {
        var positiveHits = PositiveSignals.Count(term => combined.Contains(term));

        return Math.Min(positiveHits * 4.0, 15.0);
    }

    private static double SoftPenaltyScore(string combined, SearchIntent intent)
    {
        var penaltyTerms = SoftPenalties.AsEnumerable();

        if (intent.AcceptsMining)
        {
            penaltyTerms = penaltyTerms.Where(term =>
                term is not "mining"
                and not "crypto"
                and not "ethereum"
                and not "rig"
                and not "farm"
            );
        }

        var penaltyCount = penaltyTerms.Count(term => combined.Contains(term));

        return Math.Min(penaltyCount * 5.0, 20.0);
    }

    private static double SellerScore(ProductResult product)
    {
        double score = 0;

        if (product.SellerFeedbackPercentage.HasValue)
        {
            score += product.SellerFeedbackPercentage.Value switch
            {
                >= 99 => 6,
                >= 97 => 4,
                >= 95 => 2,
                >= 90 => 0,
                _ => -5
            };
        }

        if (product.SellerFeedbackScore.HasValue)
        {
            score += product.SellerFeedbackScore.Value switch
            {
                >= 1000 => 4,
                >= 100 => 2,
                >= 10 => 0,
                _ => -2
            };
        }

        return score;
    }

    private static string? ExtractSuffix(string text)
    {
        var suffixes = new[] { "xtx", "super", "ti", "xt", "gre" };

        return suffixes.FirstOrDefault(suffix =>
            Regex.IsMatch(text, $@"\b{suffix}\b")
        );
    }

    private static string RemoveIntentWords(string query)
    {
        var removablePhrases = new[]
        {
            "for parts",
            "parts",
            "repair",
            "spares",
            "faulty",
            "broken",
            "not working",
            "no display",
            "dead",
            "cheap",
            "budget",
            "deal",
            "mining",
            "mined",
            "crypto"
        };

        foreach (var phrase in removablePhrases)
        {
            query = query.Replace(phrase, "");
        }

        return Regex.Replace(query, @"\s+", " ").Trim();
    }
}