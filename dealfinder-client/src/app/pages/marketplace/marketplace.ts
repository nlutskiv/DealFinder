import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ProductResult } from '../../models/product-result';
import { CatalogOption, ProductCategory } from '../../models/catalog-option';
import { ProductSearchService } from '../../services/product-search.service';
import { CatalogService } from '../../services/catalog.service';

type SortOption = 'score' | 'price-low' | 'price-high';

interface CategoryTab {
  key: ProductCategory;
  label: string;
}

@Component({
  selector: 'app-marketplace',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './marketplace.html'
})
export class Marketplace implements OnInit {
  categories: CategoryTab[] = [
    { key: 'gpu', label: 'GPU' },
    { key: 'cpu', label: 'CPU' },
    { key: 'ram', label: 'RAM' },
    { key: 'psu', label: 'PSU' },
    { key: 'motherboard', label: 'Motherboard' }
  ];

  selectedCategory: ProductCategory = 'gpu';
  selectedOption: CatalogOption | null = null;

  catalogOptions: CatalogOption[] = [];
  results: ProductResult[] = [];

  loadingCatalog = false;
  loading = false;
  error = '';

  sortBy: SortOption = 'score';

  constructor(
    private productSearchService: ProductSearchService,
    private catalogService: CatalogService
  ) {}

  ngOnInit(): void {
    this.loadCatalog();
  }

  get filteredOptions(): CatalogOption[] {
    return this.catalogOptions.filter(
      option => option.category === this.selectedCategory
    );
  }

  get categoryHasProducts(): boolean {
    return this.filteredOptions.length > 0;
  }

  get sortedResults(): ProductResult[] {
    return [...this.results].sort((a, b) => {
      switch (this.sortBy) {
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

  get selectedCategoryLabel(): string {
    return (
      this.categories.find(category => category.key === this.selectedCategory)
        ?.label ?? 'Products'
    );
  }

  selectCategory(category: ProductCategory): void {
    this.selectedCategory = category;
    this.selectedOption = null;
    this.results = [];
    this.error = '';
  }

  selectOption(option: CatalogOption): void {
    this.selectedOption = option;
    this.searchSelectedOption();
  }

  setSort(sortBy: SortOption): void {
    this.sortBy = sortBy;
  }

  private loadCatalog(): void {
    this.loadingCatalog = true;
    this.error = '';

    this.catalogService.getCatalog().subscribe({
      next: (catalog: CatalogOption[]) => {
        this.catalogOptions = catalog;
        this.loadingCatalog = false;
      },
      error: err => {
        console.error(err);
        this.error = 'Failed to load product catalog.';
        this.loadingCatalog = false;
      }
    });
  }

  private searchSelectedOption(): void {
    if (!this.selectedOption) {
      this.error = 'Please choose a product first.';
      return;
    }

    this.loading = true;
    this.error = '';

    this.productSearchService.search(this.selectedOption.id).subscribe({
      next: (data: ProductResult[]) => {
        this.results = data;
        this.loading = false;
      },
      error: err => {
        console.error(err);

        this.error =
          err?.error?.message ||
          'Search failed. Please try again later.';

        this.loading = false;
      }
    });
  }
}