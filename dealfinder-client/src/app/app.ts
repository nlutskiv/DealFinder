import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

import { ProductResult } from './models/product-result';
import { CatalogOption, ProductCategory } from './models/catalog-option';
import { CATALOG_OPTIONS } from './data/catalog-options';

type SortOption = 'score' | 'price-low' | 'price-high';

interface CategoryTab {
  key: ProductCategory;
  label: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  categories: CategoryTab[] = [
    { key: 'gpu', label: 'GPU' },
    { key: 'cpu', label: 'CPU' },
    { key: 'ram', label: 'RAM' },
    { key: 'psu', label: 'PSU' },
    { key: 'motherboard', label: 'Motherboard' }
  ];

  selectedCategory: ProductCategory = 'gpu';
  selectedOption: CatalogOption | null = null;

  results: ProductResult[] = [];
  loading = false;
  error = '';
  sortBy: SortOption = 'score';

  catalogOptions = CATALOG_OPTIONS;

  constructor(private http: HttpClient) {}

  get filteredOptions(): CatalogOption[] {
    return this.catalogOptions.filter(
      option => option.category === this.selectedCategory
    );
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
    return this.categories.find(c => c.key === this.selectedCategory)?.label ?? 'Products';
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

  searchSelectedOption(): void {
    if (!this.selectedOption) {
      this.error = 'Please choose a product first.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.results = [];

    this.http
      .post<ProductResult[]>('http://localhost:5185/api/ProductSearch/search', {
        query: this.selectedOption.query
      })
      .subscribe({
        next: data => {
          this.results = data;
          this.loading = false;
        },
        error: err => {
          console.error(err);
          this.error = 'Search failed. Is the backend running?';
          this.loading = false;
        }
      });
  }
}