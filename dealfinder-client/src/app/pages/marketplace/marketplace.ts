import { Component, OnInit, ViewChild, ElementRef, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Meta, Title } from '@angular/platform-browser';
import { ProductResult } from '../../models/product-result';
import { CatalogOption, ProductCategory } from '../../models/catalog-option';
import { retailerTrust, TrustLevel } from '../../models/retailer-trust';
import { ProductSearchService } from '../../services/product-search.service';
import { CatalogService } from '../../services/catalog.service';
import { PriceHistoryService, PricePoint } from '../../services/price-history.service';
import { AlertService } from '../../services/alert.service';

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
  private platformId = inject(PLATFORM_ID);

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
  hideRisky = false;

  // Price history
  priceHistory: PricePoint[] = [];
  loadingHistory = false;

  // Price alert
  alertEmail = '';
  alertTargetPrice = '';
  alertSubmitting = false;
  alertSuccess = false;
  alertError = '';

  constructor(
    private productSearchService: ProductSearchService,
    private catalogService: CatalogService,
    private priceHistoryService: PriceHistoryService,
    private alertService: AlertService,
    title: Title,
    meta: Meta
  ) {
    const t = 'PC Parts Marketplace | DealFinder';
    const d = 'Browse and compare GPU, CPU, RAM, motherboard, and storage prices across UK retailers. Set price drop alerts for any component.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ property: 'og:url', content: 'https://www.dealfinder.tech/marketplace' });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
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
    let list = this.results;
    if (this.hideRisky) {
      list = list.filter(r => this.trust(r.marketplace) === 'new');
    }
    return [...list].sort((a, b) =>
      this.sortBy === 'price-high' ? b.price - a.price : a.price - b.price
    );
  }

  trust(marketplace: string): TrustLevel {
    return retailerTrust(marketplace);
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
    this.priceHistory = [];
    this.error = '';
    this.modelSearch = '';
    this.resetAlert();
  }

  selectOption(option: CatalogOption): void {
    this.selectedOption = option;
    this.priceHistory = [];
    this.resetAlert();
    this.searchSelectedOption();
    this.loadPriceHistory(option.id);
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

  // ── Price history SVG ──────────────────────────────────────

  get sparklinePath(): string {
    const pts = this.priceHistory;
    if (pts.length < 2) return '';
    const W = 280, H = 56, PAD = 4;
    const prices = pts.map(p => p.minPrice);
    const min = Math.min(...prices);
    const max = Math.max(...prices);
    const range = max - min || 1;
    const coords = pts.map((p, i) => {
      const x = PAD + (i / (pts.length - 1)) * (W - PAD * 2);
      const y = PAD + (1 - (p.minPrice - min) / range) * (H - PAD * 2);
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    });
    return 'M ' + coords.join(' L ');
  }

  get sparklineMin(): number {
    return this.priceHistory.length ? Math.min(...this.priceHistory.map(p => p.minPrice)) : 0;
  }

  get sparklineMax(): number {
    return this.priceHistory.length ? Math.max(...this.priceHistory.map(p => p.minPrice)) : 0;
  }

  get sparklineCurrent(): number {
    return this.priceHistory.length ? this.priceHistory[this.priceHistory.length - 1].minPrice : 0;
  }

  get sparklineTrend(): 'down' | 'up' | 'flat' {
    if (this.priceHistory.length < 2) return 'flat';
    const first = this.priceHistory[0].minPrice;
    const last = this.sparklineCurrent;
    if (last < first * 0.99) return 'down';
    if (last > first * 1.01) return 'up';
    return 'flat';
  }

  // ── Alert ──────────────────────────────────────────────────

  submitAlert(): void {
    const email = this.alertEmail.trim();
    const target = parseFloat(this.alertTargetPrice);
    if (!email || isNaN(target) || target <= 0 || !this.selectedOption) return;

    const currentMin = this.selectedOption.minPrice;
    if (currentMin != null && target >= currentMin) {
      this.alertError = `Target must be below the current low of £${Math.floor(currentMin)}.`;
      return;
    }

    this.alertSubmitting = true;
    this.alertError = '';
    this.alertService.createAlert({
      email,
      catalogProductId: this.selectedOption.id,
      targetPrice: target,
    }).subscribe({
      next: () => { this.alertSuccess = true; this.alertSubmitting = false; },
      error: () => { this.alertError = 'Could not save alert. Try again.'; this.alertSubmitting = false; },
    });
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

  private loadPriceHistory(catalogId: number): void {
    this.loadingHistory = true;
    this.priceHistoryService.getHistory(catalogId).subscribe({
      next: pts => { this.priceHistory = pts; this.loadingHistory = false; },
      error: () => { this.loadingHistory = false; },
    });
  }

  private resetAlert(): void {
    this.alertEmail = '';
    this.alertTargetPrice = '';
    this.alertSuccess = false;
    this.alertError = '';
  }
}
