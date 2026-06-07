using System.Text.RegularExpressions;

namespace DealFinder.Api.Services.Classification;

public static class ProductCategoryDetector
{
    // Order matters — more specific checks first to avoid misclassification
    private static readonly (string Category, string[] Keywords, string[] Patterns)[] Rules =
    [
        (
            ProductCategory.Gpu,
            ["gpu", "graphics card", "graphics"],
            [
                @"\b(rtx|gtx)\s?\d{3,4}(\s?(ti|super))?\b",   // RTX/GTX 3080, 4070 Ti
                @"\brx\s?\d{3,4}(\s?(xt|xtx|gre))?\b",        // RX 6800 XT
                @"\barc\s?(a\d{3})\b",                          // Arc A770
                @"\bvega\s?\d+\b",                              // Vega 64
                @"\bfury\s?x?\b",                               // Fury X
                @"\bradeon\b",                                   // generic Radeon
                @"\bgeforce\b",                                  // generic GeForce
            ]
        ),
        (
            ProductCategory.Cpu,
            ["cpu", "processor"],
            [
                @"\b(i[3579]|core\s?i[3579])\s?\-?\d{4,5}[a-z]{0,2}\b",  // i7-13700K, Core i5 12600
                @"\bryzen\s?\d\s?\d{3,4}[a-z]{0,2}\b",                     // Ryzen 5 5600X
                @"\b(xeon|threadripper|epyc)\b",                            // server/HEDT
                @"\bathlon\b",                                               // AMD budget
                @"\bpentium\b",                                              // Intel budget
                @"\bceleron\b",
            ]
        ),
        (
            ProductCategory.Ram,
            ["ram", "memory"],
            [
                @"\bddr[45]\s?\-?\d{4,5}\b",          // DDR4-3200, DDR5-6000
                @"\b\d+\s?x\s?\d+\s?gb\b",            // 2x16GB, 4x8GB kit
                @"\b(dimm|so-?dimm|udimm|rdimm)\b",   // form factors
                @"\b(cl\d+|cas\s?\d+)\b",              // CL16, CAS 18 timings
            ]
        ),
        (
            ProductCategory.Psu,
            ["psu", "power supply"],
            [
                @"\b\d{3,4}\s?w(att)?\b",                          // 650W, 1000 Watt
                @"\b(80\+|80plus|80\s?plus)\s?(white|bronze|silver|gold|platinum|titanium)?\b",
                @"\b(atx|sfx|tfx)\s?(psu|power)?\b",               // form factors
                @"\b(modular|semi.?modular|non.?modular)\b",
                @"\b(seasonic|corsair|evga|bequiet|be quiet|fractal|antec|thermaltake)\b",
            ]
        ),
        (
            ProductCategory.Motherboard,
            ["motherboard", "mobo", "mainboard"],
            [
                // Intel chipsets
                @"\b(z[679]\d0|h[67]\d0|b[67]\d0|w[67]\d0)\b",
                // AMD chipsets
                @"\b(x[356][57]0|b[345][56]0|a[34][25]0|x670e?|b650e?)\b",
                // Sockets
                @"\b(am[245]\+?|lga\s?\d{4}|tr[45]|sp[35])\b",
                // Brands
                @"\b(asus|gigabyte|msi|asrock|evga)\s.*(board|mobo)?\b",
            ]
        ),
        (
            ProductCategory.Storage,
            ["ssd", "hdd", "hard drive", "nvme", "storage"],
            [
                @"\b(m\.?2|2\.5|3\.5)\s?(ssd|hdd|nvme|sata)?\b",
                @"\b\d+\s?(tb|gb)\s?(ssd|hdd|nvme|drive|disk)\b",
                @"\b(nvme|sata|pcie)\s?(ssd|drive)?\b",
                @"\b(wd|seagate|samsung|crucial|kingston|sabrent)\b",
            ]
        ),
        (
            ProductCategory.Cooling,
            ["cooler", "cooling", "fan", "aio", "radiator"],
            [
                @"\b(aio|liquid\s?cool|water\s?cool)\b",
                @"\b\d+\s?mm\s?(fan|radiator)\b",            // 240mm, 360mm
                @"\b(noctua|be\s?quiet|arctic|corsair|nzxt)\b",
                @"\b(air\s?cool|tower\s?cooler|cpu\s?fan)\b",
            ]
        ),
    ];

    public static string Detect(string query)
    {
        var q = query.ToLowerInvariant().Trim();

        foreach (var (category, keywords, patterns) in Rules)
        {
            // keyword match first (fast path)
            if (keywords.Any(k => q.Contains(k)))
                return category;

            // regex match (catches model numbers without category words)
            if (patterns.Any(p => Regex.IsMatch(q, p, RegexOptions.IgnoreCase)))
                return category;
        }

        return ProductCategory.Generic;
    }

    // Confidence score — useful later for ambiguous queries
    public static (string Category, double Confidence) DetectWithConfidence(string query)
    {
        var q = query.ToLowerInvariant().Trim();
        var scores = new Dictionary<string, int>();

        foreach (var (category, keywords, patterns) in Rules)
        {
            var hits = keywords.Count(k => q.Contains(k))
                     + patterns.Count(p => Regex.IsMatch(q, p, RegexOptions.IgnoreCase));

            if (hits > 0) scores[category] = hits;
        }

        if (scores.Count == 0)
            return (ProductCategory.Generic, 0.0);

        var best = scores.MaxBy(x => x.Value);
        var confidence = Math.Min(best.Value / 3.0, 1.0); // 3+ hits = 100% confident

        return (best.Key, confidence);
    }
}