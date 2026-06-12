import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { ProductResult } from '../../models/product-result';
import { CatalogOption, ProductCategory } from '../../models/catalog-option';
import { ProductSearchService } from '../../services/product-search.service';
import { CatalogService } from '../../services/catalog.service';

type SortOption = 'price-low' | 'price-high';

interface CategoryTab {
  key: ProductCategory;
  label: string;
}

@Component({
  selector: 'app-marketplace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './marketplace.html',
  styleUrl: './marketplace.css'
})
export class Marketplace implements OnInit {
  @ViewChild('resultsSection') resultsSection!: ElementRef;

  categories: CategoryTab[] = [
    { key: 'gpu', label: 'GPU' },
    { key: 'cpu', label: 'CPU' },
    { key: 'ram', label: 'RAM' },
    { key: 'psu', label: 'PSU' },
    { key: 'motherboard', label: 'Motherboard' },
    { key: 'storage', label: 'Storage' },
    { key: 'cooler', label: 'Cooler' },
    { key: 'case', label: 'Case' }
  ];

  selectedCategory: ProductCategory = 'gpu';
  selectedOption: CatalogOption | null = null;

  catalogOptions: CatalogOption[] = [];
  results: ProductResult[] = [];

  modelSearch = '';
  loadingCatalog = false;
  loading = false;
  error = '';

  sortBy: SortOption = 'price-low';

  constructor(
    private productSearchService: ProductSearchService,
    private catalogService: CatalogService,
    title: Title
  ) {
    title.setTitle('PC Parts Marketplace | DealFinder');
  }

  ngOnInit(): void {
    this.loadCatalog();
  }

  get filteredOptions(): CatalogOption[] {
    const search = this.modelSearch.trim().toLowerCase();
    return this.catalogOptions
      .filter(o => o.category === this.selectedCategory)
      .filter(o => !search || o.label.toLowerCase().includes(search));
  }

  get categoryHasProducts(): boolean {
    return this.catalogOptions.some(o => o.category === this.selectedCategory);
  }

  get sortedResults(): ProductResult[] {
    return [...this.results].sort((a, b) =>
      this.sortBy === 'price-high' ? b.price - a.price : a.price - b.price
    );
  }

  get selectedCategoryLabel(): string {
    return this.categories.find(c => c.key === this.selectedCategory)?.label ?? 'Products';
  }

  listingWord(count: number): string {
    return count === 1 ? 'listing' : 'listings';
  }

  selectCategory(category: ProductCategory): void {
    this.selectedCategory = category;
    this.selectedOption = null;
    this.results = [];
    this.error = '';
    this.modelSearch = '';
  }

  selectOption(option: CatalogOption): void {
    this.selectedOption = option;
    this.searchSelectedOption();
  }

  setSort(sortBy: SortOption): void {
    this.sortBy = sortBy;
  }

  priceRange(option: CatalogOption): string | null {
    if (option.minPrice == null || option.maxPrice == null) return null;
    const lo = Math.round(option.minPrice);
    const hi = Math.round(option.maxPrice);
    return lo === hi ? `£${lo}` : `£${lo} – £${hi}`;
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
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
    if (!this.selectedOption) return;
    this.loading = true;
    this.error = '';
    this.productSearchService.search(this.selectedOption.id).subscribe({
      next: (data: ProductResult[]) => {
        this.results = data;
        this.loading = false;
        setTimeout(() => {
          this.resultsSection?.nativeElement?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 80);
      },
      error: err => {
        console.error(err);
        this.error = err?.error?.message || 'Search failed. Please try again later.';
        this.loading = false;
      }
    });
  }
}
