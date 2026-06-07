using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public class CpuPromptBuilder
{
    public string BuildEvaluationPrompt(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    )
    {
        var description = Trim(product.Description);
        var meanPriceText = estimatedMeanPrice.HasValue
            ? $"£{estimatedMeanPrice.Value:F2}"
            : "unknown";

        return $@"
Evaluate this used CPU listing.

Use only the listing data. Return only valid JSON.

User query: ""{request.Query}""

Listing:
- Title: {product.Title}
- Description: {description}
- Price: £{product.Price}
- Shipping: £{product.ShippingPrice}
- Total price: £{product.Price + product.ShippingPrice}
- Seller: {product.SellerName}
- Seller feedback %: {product.SellerFeedbackPercentage}
- Seller feedback score: {product.SellerFeedbackScore}
- Marketplace: {product.Marketplace}
- Mean price for similar candidates: {meanPriceText}

Score independently:
- query_match_score: exact CPU model match. Wrong model/category = low.
- condition_score: working/testing evidence only.
- seller_trust_score: seller trust only.
- risk_score: buyer risk only. 0 low risk, 100 high risk.
- value_score: price vs mean price only, with obvious fault/scam penalty.

CPU-specific rules:
Reward: tested working, no bent pins, stable in BIOS/Windows, stress tested, original box, receipt, returns, strong seller.
Penalize: bent pins, missing pins, socket damage, overheating, instability, untested, pulled from unknown system, sold as seen, no returns, vague claims.
For AM4/older Ryzen, bent pins are serious. For Intel/LGA CPUs, pad damage is serious. For AM5/LGA platforms, motherboard socket pins matter more than CPU pins.
Cheap but faulty/scammy listings must not get high value_score.
vram_gb must be null for CPUs.

JSON shape:
{{
  ""model"": ""detected CPU model or unknown"",
  ""vram_gb"": null,
  ""query_match_score"": 85,
  ""condition"": ""short condition summary"",
  ""condition_score"": 65,
  ""seller_trust_score"": 50,
  ""risk_score"": 35,
  ""risk_label"": ""LOW"",
  ""value_score"": 60,
  ""detected_issues"": [""short issue""],
  ""positive_signals"": [""short signal""],
  ""reasoning"": ""one short sentence""
}}";
    }

    private static string Trim(string value)
    {
        return value.Length > 500 ? value[..500] : value;
    }
}