import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { RankingsService, RankingItem } from '../../services/rankings.service';
import { amazonSearchLink } from '../../models/amazon-link';

const CATEGORY_ORDER = ['gpu', 'cpu', 'motherboard', 'ram', 'storage', 'psu', 'case', 'cooler'];

const CATEGORY_META: Record<string, { title: string; description: string }> = {
  gpu: {
    title: 'Best Value GPUs Today UK | DealFinder',
    description: 'Best graphics card deals in the UK right now, ranked by performance per pound from live retailer prices.',
  },
  cpu: {
    title: 'Best Value CPUs Today UK | DealFinder',
    description: 'Best processor deals in the UK right now, ranked by performance per pound from live retailer prices.',
  },
  motherboard: {
    title: 'Cheapest Motherboards Today UK | DealFinder',
    description: "Today's cheapest motherboards across UK retailers, updated from live feeds.",
  },
  ram: {
    title: 'Cheapest RAM Today UK | DealFinder',
    description: "Today's cheapest DDR4 and DDR5 memory kits across UK retailers.",
  },
  storage: {
    title: 'Cheapest SSDs & Storage Today UK | DealFinder',
    description: "Today's cheapest SSDs and storage drives across UK retailers.",
  },
  psu: {
    title: 'Cheapest PSUs Today UK | DealFinder',
    description: "Today's cheapest power supplies across UK retailers.",
  },
  case: {
    title: 'Cheapest PC Cases Today UK | DealFinder',
    description: "Today's cheapest PC cases across UK retailers.",
  },
  cooler: {
    title: 'Cheapest CPU Coolers Today UK | DealFinder',
    description: "Today's cheapest CPU coolers across UK retailers.",
  },
};

@Component({
  selector: 'app-rankings',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './rankings.html',
  styleUrl: './rankings.css'
})
export class Rankings implements OnInit {
  private platformId = inject(PLATFORM_ID);

  items: RankingItem[] = [];
  loading = true;
  error = '';

  activeCategory = 'gpu';
  minPrice = 0;
  hideRisky = false;

  readonly pricePresets = [
    { label: 'Any', value: 0 },
    { label: '£20+', value: 20 },
    { label: '£50+', value: 50 },
    { label: '£100+', value: 100 },
    { label: '£200+', value: 200 },
    { label: '£500+', value: 500 },
  ];

  readonly categories = [
    { key: 'gpu', label: 'GPU' },
    { key: 'cpu', label: 'CPU' },
    { key: 'motherboard', label: 'Motherboard' },
    { key: 'ram', label: 'RAM' },
    { key: 'storage', label: 'Storage' },
    { key: 'psu', label: 'PSU' },
    { key: 'case', label: 'Case' },
    { key: 'cooler', label: 'Cooler' },
  ];

  constructor(
    private rankingsService: RankingsService,
    private route: ActivatedRoute,
    private router: Router,
    private title: Title,
    private meta: Meta
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const cat = params.get('category');
      if (cat && CATEGORY_ORDER.includes(cat)) {
        this.activeCategory = cat;
      }
      this.setMeta();
    });

    if (!isPlatformBrowser(this.platformId)) return;
    this.rankingsService.getRankings().subscribe({
      next: items => {
        this.items = [...items].sort(
          (a, b) => CATEGORY_ORDER.indexOf(a.category) - CATEGORY_ORDER.indexOf(b.category)
        );
        this.loading = false;
      },
      error: () => { this.error = 'Could not load rankings.'; this.loading = false; }
    });
  }

  selectCategory(key: string): void {
    this.activeCategory = key;
    this.minPrice = 0;
    this.router.navigate(['/rankings', key]);
  }

  get visibleItems(): RankingItem[] {
    let list = this.items.filter(i =>
      i.category === this.activeCategory &&
      this.effectivePrice(i) >= this.minPrice
    );

    if (!this.hideRisky) {
      return list;
    }

    // When hiding risky sellers, drop only models that have NO trusted listing,
    // then re-rank by the trusted price/value (the backend ranked by overall price,
    // which may have used a risky listing).
    list = list.filter(i => i.trustedMinPrice != null);

    const valueRanked = list.some(i => i.valueScore != null);
    return [...list].sort((a, b) => {
      if (valueRanked) {
        return this.trustedValue(b) - this.trustedValue(a);
      }
      return this.effectivePrice(a) - this.effectivePrice(b);
    });
  }

  /** Price shown for a row, honouring the hide-risky toggle. */
  effectivePrice(item: RankingItem): number {
    if (this.hideRisky && item.trustedMinPrice != null) return item.trustedMinPrice;
    return item.minPrice;
  }

  effectiveUrl(item: RankingItem): string {
    return this.hideRisky && item.trustedTopListingUrl ? item.trustedTopListingUrl : item.topListingUrl;
  }

  effectiveImage(item: RankingItem): string {
    return (this.hideRisky && item.trustedTopListingImageUrl
      ? item.trustedTopListingImageUrl
      : item.topListingImageUrl) || '/fallback-product.svg';
  }

  effectiveMarketplace(item: RankingItem): string {
    return this.hideRisky && item.trustedTopListingMarketplace
      ? item.trustedTopListingMarketplace
      : item.topListingMarketplace;
  }

  private trustedValue(item: RankingItem): number {
    if (item.performanceScore != null && item.trustedMinPrice) {
      return item.performanceScore / item.trustedMinPrice;
    }
    return -1;
  }

  get isValueRanked(): boolean {
    return this.visibleItems.some(i => i.valueScore != null);
  }

  productLink(item: RankingItem): string[] {
    return ['/product', String(item.catalogProductId), this.slugify(item.modelLabel)];
  }

  amazonLink(item: RankingItem): string {
    return amazonSearchLink(item.modelLabel);
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
  }

  private slugify(label: string): string {
    return label.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
  }

  private setMeta(): void {
    const m = CATEGORY_META[this.activeCategory] ?? CATEGORY_META['gpu'];
    this.title.setTitle(m.title);
    this.meta.updateTag({ name: 'description', content: m.description });
    this.meta.updateTag({ property: 'og:title', content: m.title });
    this.meta.updateTag({ property: 'og:description', content: m.description });
    this.meta.updateTag({ property: 'og:url', content: `https://www.dealfinder.tech/rankings/${this.activeCategory}` });
    this.meta.updateTag({ name: 'twitter:title', content: m.title });
    this.meta.updateTag({ name: 'twitter:description', content: m.description });
  }
}
