// 'used' = seller deals in used/refurbished stock (often cheapest, may show wear).
// 'new'  = sells new stock.
export type TrustLevel = 'used' | 'new';

// Marketplaces that sell used/refurbished stock.
// Keep in sync with RiskyMarketplaces in DealFinder.Api/Services/Rankings/RankingsService.cs.
const USED_REFURB = new Set([
  'lovetechhatewaste', 'lovetechhatewaste.com', 'love tech hate waste',
]);

export function retailerTrust(marketplace: string): TrustLevel {
  return USED_REFURB.has(marketplace.toLowerCase().trim()) ? 'used' : 'new';
}
