using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public class GpuPromptBuilder : IGpuPromptBuilder
{
    public string BuildEvaluationPrompt(ProductSearchRequest request, ProductResult product)
    {
        return $@"
You are evaluating used GPU marketplace listings.

User search query:
""{request.Query}""

Listing data:
- Title: {product.Title}
- Description: {product.Description}
- Price: £{product.Price}
- Shipping: £{product.ShippingPrice}
- Marketplace: {product.Marketplace}
- Condition field: {product.Condition}

Evaluate:
- Identify the most likely GPU model.
- Extract VRAM if mentioned.
- query_match_score: 0-100 based on how relevant this listing is to the user's search.
- value_score: 0-100 based on value for money.
- risk_level: 0-100 where higher means riskier.
- Penalize faulty, broken, for parts, untested, no display, artifacting, mining, repair, spares, no returns.
- Do not force exact matches. A different GPU can still receive a decent query_match_score if it may be relevant.
- Be conservative if uncertain.

Return ONLY valid JSON:

{{
  ""model"": ""RTX 3080"",
  ""vram_gb"": 10,
  ""query_match_score"": 90,
  ""condition"": ""good"",
  ""risk_level"": 15,
  ""value_score"": 82,
  ""reasoning"": ""Strong RTX 3080 listing with realistic pricing and no obvious red flags.""
}}
";
    }
}