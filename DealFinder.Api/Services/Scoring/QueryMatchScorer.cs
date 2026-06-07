using DealFinder.Api.Models;
using DealFinder.Api.Services.Classification;

namespace DealFinder.Api.Services.Scoring;

public static class QueryMatchScorer
{
    public static double Calculate(string query, ProductResult product)
    {
        var detectedCategory = ProductCategoryDetector.Detect(query);

        if (!product.Category.Equals(detectedCategory, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var queryTokens = ExtractUsefulTokens(query, detectedCategory);
        var productText = $"{product.Title} {product.Description}".ToLowerInvariant();

        // Broad search like "cpu", "gpu", "processor"
        if (queryTokens.Count == 0)
        {
            return 100;
        }

        var matchedTokens = queryTokens.Count(token =>
            productText.Contains(token)
        );

        if (matchedTokens == queryTokens.Count)
        {
            return 100;
        }

        if (matchedTokens > 0)
        {
            return 50;
        }

        return 15;
    }

    private static List<string> ExtractUsefulTokens(string query, string category)
    {
        var stopWords = new HashSet<string>
        {
            "for", "parts", "part", "repair", "spares", "faulty", "broken",
            "not", "working", "cheap", "budget", "deal", "deals", "used",
            "new", "gaming", "pc", "card", "graphics"
        };

        if (category == ProductCategory.Cpu)
        {
            stopWords.Add("cpu");
            stopWords.Add("processor");
        }

        if (category == ProductCategory.Gpu)
        {
            stopWords.Add("gpu");
            stopWords.Add("graphics");
            stopWords.Add("card");
        }

        if (category == ProductCategory.Ram)
        {
            stopWords.Add("ram");
            stopWords.Add("memory");
        }

        if (category == ProductCategory.Psu)
        {
            stopWords.Add("psu");
            stopWords.Add("power");
            stopWords.Add("supply");
        }

        if (category == ProductCategory.Motherboard)
        {
            stopWords.Add("motherboard");
            stopWords.Add("mobo");
        }

        return query
            .ToLowerInvariant()
            .Replace("-", " ")
            .Replace("_", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim())
            .Where(token => token.Length > 1)
            .Where(token => !stopWords.Contains(token))
            .ToList();
    }
}