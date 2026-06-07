using System.Text.RegularExpressions;

namespace DealFinder.Api.Services.Classification;

public static class ProductCategoryDetector
{
    public static string Detect(string query)
    {
        var q = query.ToLowerInvariant();

        if (Regex.IsMatch(q, @"\b(rtx|gtx|radeon|rx|arc|gpu|graphics|graphics card)\b"))
            return ProductCategory.Gpu;

        if (Regex.IsMatch(q, @"\b(i3|i5|i7|i9|ryzen|xeon|threadripper|cpu|processor)\b"))
            return ProductCategory.Cpu;

        if (Regex.IsMatch(q, @"\b(ddr4|ddr5|ram|memory|\d+gb ram)\b"))
            return ProductCategory.Ram;

        if (Regex.IsMatch(q, @"\b(psu|power supply|\d+w|watt)\b"))
            return ProductCategory.Psu;

        if (Regex.IsMatch(q, @"\b(motherboard|mobo|b650|b550|x570|x670|z690|z790|a520|h610)\b"))
            return ProductCategory.Motherboard;

        return ProductCategory.Generic;
    }
}