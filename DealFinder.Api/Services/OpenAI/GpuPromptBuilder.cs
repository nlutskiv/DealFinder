using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public class GpuPromptBuilder : IGpuPromptBuilder
{
    public string BuildEvaluationPrompt(ProductResult product)
{
    return $@"
You are evaluating a used GPU marketplace listing.

Target:
The user is searching for a GPU deal.

Listing data:
- Title: {product.Title}
- Description: {product.Description}
- Price: £{product.Price}
- Shipping: £{product.ShippingPrice}
- Condition field: {product.Condition}
- Seller: {product.SellerName}
- Seller feedback percentage: {product.SellerFeedbackPercentage}
- Seller feedback score: {product.SellerFeedbackScore}
- Marketplace: {product.Marketplace}

Evaluate:
- Does this listing actually appear to be a GPU?
- What GPU model is most likely being sold?
- Is there any evidence of faults, mining, repairs, missing parts, scam wording, or misleading title?
- Is the price reasonable for the likely GPU model?
- Should a buyer be cautious?

Return ONLY valid JSON:

{{
  ""match"": true,
  ""model"": ""RTX 3080"",
  ""vram_gb"": 10,
  ""condition"": ""good"",
  ""risk_level"": 15,
  ""value_score"": 82,
  ""reasoning"": ""Good-condition RTX 3080 listing with realistic pricing and no obvious red flags.""
}}
";
}
}