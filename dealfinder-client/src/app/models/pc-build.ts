export interface PcBuildPart {
  category: string;
  categoryLabel: string;
  listingId: number;
  title: string;
  price: number;
  shippingPrice: number;
  url: string;
  imageUrl: string;
  marketplace: string;
  condition: string;
}

export interface PcBuildResult {
  budget: number;
  totalPrice: number;
  parts: PcBuildPart[];
  aiSummary: string;
  compatibilityWarnings: string[];
  isCompatible: boolean;
}
