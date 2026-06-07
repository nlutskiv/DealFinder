using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public class GpuPromptBuilder
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
Evaluate this used GPU listing.

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
- query_match_score: exact GPU model match. Wrong model/category = low.
- condition_score: working/testing evidence only.
- seller_trust_score: seller trust only.
- risk_score: buyer risk only. 0 low risk, 100 high risk.
- value_score: price vs mean price only, with obvious scam/fault penalty.

GPU-specific rules:
Reward: tested working, 3DMark/FurMark/Heaven/game testing, no artifacting, no crashes, no overheating, stable temps, warranty/receipt/returns, strong seller.
Penalize: mining, no display, artifacting, crashes, overheating, broken/noisy fans, damaged PCB, missing parts, untested, spares/repair, sold as seen, no returns, vague claims.
Cheap but faulty/scammy listings must not get high value_score.

JSON shape:
{{
  ""model"": ""detected GPU model or unknown"",
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