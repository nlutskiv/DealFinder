export type ProductCategory = 'gpu' | 'cpu' | 'ram' | 'psu' | 'motherboard';

export interface CatalogOption {
  category: ProductCategory;
  label: string;
  query: string;
  description?: string;
}