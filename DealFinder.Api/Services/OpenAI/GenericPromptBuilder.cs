using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public static class GenericPromptBuilder
{
    public static string BuildEvaluationPrompt(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    )
    {
        var description = product.Description.Length > 500
            ? product.Description[..500]
            : product.Description;

        var meanPriceText = estimatedMeanPrice.HasValue
            ? $"£{estimatedMeanPrice.Value:F2}"
            : "unknown";

        return $@"
Evaluate this used PC hardware listing.

Use only the listing data. Return only valid JSON.

User query: ""{request.Query}""

Listing:
- Category: {product.Category}
- Title: {product.Title}
- Description: {description}
- Total price: £{product.Price + product.ShippingPrice}
- Seller feedback %: {product.SellerFeedbackPercentage}
- Seller feedback score: {product.SellerFeedbackScore}
- Mean price for similar candidates: {meanPriceText}

Score query match, condition, seller trust, risk, and value independently.

JSON shape:
{{
  ""model"": ""detected model or unknown"",
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
}