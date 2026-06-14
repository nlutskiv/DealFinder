export type TrustLevel = 'risky' | 'ok';

const RISKY = new Set([
  'lovetechhatewaste', 'lovetechhatewaste.com', 'love tech hate waste',
]);

export function retailerTrust(marketplace: string): TrustLevel {
  return RISKY.has(marketplace.toLowerCase().trim()) ? 'risky' : 'ok';
}
