import { ProductResult } from '../models/product-result';

export type SortOption = 'score' | 'price-low' | 'price-high';

export function sortProducts(
  products: ProductResult[],
  sortBy: SortOption
): ProductResult[] {
  return [...products].sort((a, b) => {
    switch (sortBy) {
      case 'price-low':
        return a.price - b.price;

      case 'price-high':
        return b.price - a.price;

      case 'score':
      default:
        return b.score - a.score;
    }
  });
}