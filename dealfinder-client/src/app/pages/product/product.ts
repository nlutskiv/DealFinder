import { ChangeDetectorRef, Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule, DOCUMENT } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { ProductResult } from '../../models/product-result';
import { CatalogOption } from '../../models/catalog-option';
import { ProductSearchService } from '../../services/product-search.service';
import { CatalogService } from '../../services/catalog.service';
import { PriceHistoryService, PricePoint } from '../../services/price-history.service';
import { AlertService } from '../../services/alert.service';

@Component({
  selector: 'app-product',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product.html',
  styleUrl: './product.css'
})
export class ProductPage implements OnInit {
  private platformId = inject(PLATFORM_ID);
  private cdr = inject(ChangeDetectorRef);
  private document = inject(DOCUMENT);

  option: CatalogOption | null = null;
  results: ProductResult[] = [];
  priceHistory: PricePoint[] = [];
  loading = true;
  error = '';
  notFound = false;

  alertEmail = '';
  alertTargetPrice = '';
  alertSubmitting = false;
  alertSuccess = false;
  alertError = '';

  constructor(
    private route: ActivatedRoute,
    private productSearchService: ProductSearchService,
    private catalogService: CatalogService,
    private priceHistoryService: PriceHistoryService,
    private alertService: AlertService,
    private title: Title,
    private meta: Meta
  ) {}

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.route.paramMap.subscribe(params => {
      const id = parseInt(params.get('id') ?? '', 10);
      if (isNaN(id) || id <= 0) {
        this.notFound = true;
        this.loading = false;
        return;
      }
      this.load(id);
    });
  }

  get sortedResults(): ProductResult[] {
    return [...this.results].sort(
      (a, b) => (a.price + a.shippingPrice) - (b.price + b.shippingPrice)
    );
  }

  get bestPrice(): number | null {
    const sorted = this.sortedResults;
    return sorted.length ? sorted[0].price + sorted[0].shippingPrice : null;
  }

  get retailerCount(): number {
    return new Set(this.results.map(r => r.marketplace)).size;
  }

  // % below 30-day average, shown only when meaningful (>=5 days of history, >=5% drop)
  get dropPercent(): number | null {
    if (this.priceHistory.length < 5) return null;
    const avg = this.priceHistory.reduce((s, p) => s + p.minPrice, 0) / this.priceHistory.length;
    const cur = this.bestPrice;
    if (cur == null || avg <= 0) return null;
    const drop = Math.round((1 - cur / avg) * 100);
    return drop >= 5 ? drop : null;
  }

  totalPrice(r: ProductResult): number {
    return r.price + r.shippingPrice;
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
  }

  submitAlert(): void {
    const email = this.alertEmail.trim();
    const target = parseFloat(this.alertTargetPrice);
    if (!email || isNaN(target) || target <= 0 || !this.option) return;

    const currentMin = this.bestPrice;
    if (currentMin != null && target >= currentMin) {
      this.alertError = `Target must be below the current low of £${Math.floor(currentMin)}.`;
      return;
    }

    this.alertSubmitting = true;
    this.alertError = '';
    this.alertService.createAlert({
      email,
      catalogProductId: this.option.id,
      targetPrice: target,
    }).subscribe({
      next: () => { this.alertSuccess = true; this.alertSubmitting = false; },
      error: () => { this.alertError = 'Could not save alert. Try again.'; this.alertSubmitting = false; },
    });
  }

  private load(id: number): void {
    this.loading = true;
    this.catalogService.getCatalog().subscribe({
      next: (catalog: CatalogOption[]) => {
        const option = catalog.find(o => o.id === id) ?? null;
        if (!option) {
          this.notFound = true;
          this.loading = false;
          this.cdr.detectChanges();
          return;
        }
        this.option = option;
        this.setMeta(option);
        this.search(option.id);
        this.priceHistoryService.getHistory(option.id).subscribe({
          next: pts => { this.priceHistory = pts; this.cdr.detectChanges(); },
          error: () => {},
        });
      },
      error: () => {
        this.error = 'Failed to load product.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private search(id: number): void {
    this.productSearchService.search(id).subscribe({
      next: (data: ProductResult[]) => {
        this.results = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Could not load listings.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private setMeta(option: CatalogOption): void {
    const price = option.minPrice != null ? ` from £${Math.floor(option.minPrice)}` : '';
    const t = `${option.label} Price Comparison UK${price} | DealFinder`;
    const d = `Compare live ${option.label} prices across UK retailers. ${option.listingCount} listings tracked${price}. Price history and drop alerts included.`;
    this.title.setTitle(t);
    this.meta.updateTag({ name: 'description', content: d });
    this.meta.updateTag({ property: 'og:title', content: t });
    this.meta.updateTag({ property: 'og:description', content: d });
    this.meta.updateTag({ name: 'twitter:title', content: t });
    this.meta.updateTag({ name: 'twitter:description', content: d });
    this.setJsonLd(option);
  }

  // Product schema with live offer data — lets Google show price directly in search results.
  private setJsonLd(option: CatalogOption): void {
    const id = 'product-jsonld';
    this.document.getElementById(id)?.remove();
    if (option.minPrice == null) return;

    const schema = {
      '@context': 'https://schema.org',
      '@type': 'Product',
      name: option.label,
      category: option.category.toUpperCase(),
      offers: {
        '@type': 'AggregateOffer',
        priceCurrency: 'GBP',
        lowPrice: option.minPrice,
        highPrice: option.maxPrice ?? option.minPrice,
        offerCount: option.listingCount,
        availability: 'https://schema.org/InStock',
      },
    };

    const script = this.document.createElement('script');
    script.id = id;
    script.type = 'application/ld+json';
    script.textContent = JSON.stringify(schema);
    this.document.head.appendChild(script);
  }
}
