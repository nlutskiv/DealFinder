export type ProductCategory = 'gpu' | 'cpu' | 'ram' | 'psu' | 'motherboard';

export interface CatalogOption {
  id: number;
  category: ProductCategory;
  label: string;
  description?: string;
  listingCount: number;
}