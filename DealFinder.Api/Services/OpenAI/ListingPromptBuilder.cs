using DealFinder.Api.Models;

namespace DealFinder.Api.Services.OpenAI;

public class ListingPromptBuilder : IListingPromptBuilder
{
    public string BuildEvaluationPrompt(
        ProductSearchRequest request,
        ProductResult product,
        decimal? estimatedMeanPrice
    )
    {
        var description = product.Description.Length > 600
            ? product.Description[..600]
            : product.Description;

        var meanPriceText = estimatedMeanPrice.HasValue
            ? $"£{estimatedMeanPrice.Value:F2}"
            : "unknown";

        return $@"
You evaluate used PC hardware marketplace listings.

Use only the provided data. Do not assume unstated facts.
Return only valid JSON. No markdown.

User query:
""{request.Query}""

Listing:
- Category: {product.Category}
- Title: {product.Title}
- Description: {description}
- Price: £{product.Price}
- Shipping: £{product.ShippingPrice}
- Total price: £{product.Price + product.ShippingPrice}
- Condition: {product.Condition}
- Seller: {product.SellerName}
- Seller feedback %: {product.SellerFeedbackPercentage}
- Seller feedback score: {product.SellerFeedbackScore}
- Marketplace: {product.Marketplace}
- Estimated mean price for matching listings: {meanPriceText}

Evaluate separate independent factors:

query_match_score:
- Only measures how well the listing matches the user's requested product.
- Do not include price, condition, seller trust, or risk.
- Exact product/model match should be high.
- Wrong category or wrong product type should be 0.

condition_score:
- Only measures stated physical/working condition and testing evidence.
- Reward tested working, benchmarks/stress tests, stable operation, no known faults.
- Penalize faulty, no display, bent pins, overheating, broken/noisy fans, damaged PCB, missing parts, spares/repair, untested.
- For CPUs, bent pins/socket damage are serious risks.
- For GPUs, artifacting, no display, mining history, overheating, and fan faults are serious risks.
- Do not include price or seller feedback.

seller_trust_score:
- Only measures seller/listing trust.
- Reward strong feedback, many reviews, returns, warranty, receipt, buyer protection, trusted marketplace.
- Penalize missing seller info, poor feedback, no returns, sold as seen.
- Do not include price or product condition.

risk_score:
- Only measures buyer risk and red flags.
- 0 = very low risk, 100 = very high risk.
- Penalize faulty, repair/spares, untested, vague wording, suspicious claims, misleading model names, missing key details.
- Do not include whether the price is good unless the price is suspiciously low.

value_score:
- Mostly measures price attractiveness compared with the estimated mean price.
- If total price is below mean, value can be higher.
- If total price is above mean, value should be lower.
- If estimated mean price is unknown, estimate value cautiously from listing context.
- Do not heavily include seller trust, condition, or query match here.
- Exception: clearly faulty/scam/extremely risky listings should not get high value_score.

Important:
- Keep the subscores separate. Do not fold everything into value_score.
- Missing information should reduce the relevant score.
- Do not return 0 unless that factor is genuinely terrible.
- risk_label must be LOW, MEDIUM, or HIGH.
- Keep reasoning short and specific.

Use these exact JSON property names:

{{
  ""model"": ""detected product model or unknown"",
  ""vram_gb"": null,
  ""query_match_score"": 85,
  ""condition"": ""short condition summary"",
  ""condition_score"": 65,
  ""seller_trust_score"": 50,
  ""risk_score"": 35,
  ""risk_label"": ""LOW"",
  ""value_score"": 60,
  ""detected_issues"": [""short issue string""],
  ""positive_signals"": [""short positive signal string""],
  ""reasoning"": ""one short sentence explaining the main tradeoff""
}}
";
    }
}