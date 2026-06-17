import { environment } from '../../environments/environment';

// Builds a tagged Amazon.co.uk search link for a product model.
// Amazon allows tagged search-result links, the affiliate cookie sets on click,
// and anything bought within 24h earns commission. No API/SiteStripe needed.
export function amazonSearchLink(modelLabel: string): string {
  const query = encodeURIComponent((modelLabel ?? '').trim());
  return `https://www.amazon.co.uk/s?k=${query}&tag=${environment.amazonAffiliateTag}`;
}
