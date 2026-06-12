export type ProductCategory = 'gpu' | 'cpu' | 'ram' | 'psu' | 'motherboard' | 'cooler' | 'case' | 'storage';

export interface CatalogOption {
  id: number;
  category: ProductCategory;
  label: string;
  listingCount: number;
  minPrice?: number;
  maxPrice?: number;
}
