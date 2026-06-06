using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public class GpuPromptBuilder : IGpuPromptBuilder
{
    public string BuildEvaluationPrompt(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    )
    {
        var description = product.Description.Length > 400
            ? product.Description[..400]
            : product.Description;

        var meanPriceText = estimatedMeanPrice.HasValue
            ? $"£{estimatedMeanPrice.Value:F2}"
            : "unknown";

        var totalPrice = product.Price + product.ShippingPrice;

        return $@"Evaluate this used GPU listing. Return only valid JSON, no markdown.

Query: ""{request.Query}""
Title: {product.Title}
Description: {description}
Total price: £{totalPrice} (item £{product.Price} + shipping £{product.ShippingPrice})
Condition: {product.Condition}
Seller: {product.SellerName} | feedback: {product.SellerFeedbackPercentage}% ({product.SellerFeedbackScore} reviews)
Marketplace: {product.Marketplace}
Mean market price: {meanPriceText}

Score each factor independently (0-100):
- query_match_score: GPU model match only. Ignore price/condition/risk.
- condition_score: Physical/working condition evidence only. Reward: tested, benchmarked, no faults. Penalise: faulty, untested, artifacting, overheating, damaged fans/PCB.
- seller_trust_score: Seller reliability only. Reward: high feedback, returns, warranty, receipt. Penalise: no returns, poor/missing feedback.
- risk_score: Buyer risk only (0=none, 100=extreme). Flag: mining, no display, faults, spares/repair, vague/suspicious claims, suspiciously low price.
- value_score: Price vs mean market price. Below mean = higher score, above mean = lower. Unknown mean = estimate cautiously. Exception: scam/extremely risky listings cap at 30.

Rules:
- Never fold multiple factors into one score.
- Missing info lowers the relevant score.
- Never return 0 unless genuinely terrible.
- risk_label: ""LOW"" <25, ""MEDIUM"" 25-59, ""HIGH"" 60-79, ""CRITICAL"" 80+

{{
  ""model"": ""GPU model or unknown"",
  ""vram_gb"": null,
  ""query_match_score"": 0,
  ""condition"": ""one phrase"",
  ""condition_score"": 0,
  ""seller_trust_score"": 0,
  ""risk_score"": 0,
  ""risk_label"": ""LOW"",
  ""value_score"": 0,
  ""detected_issues"": [],
  ""positive_signals"": [],
  ""reasoning"": ""one sentence""
}}";
    }
}