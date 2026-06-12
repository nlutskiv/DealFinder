import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { RankingsService, RankingItem } from '../../services/rankings.service';

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
    this.router.navigate(['/rankings', key]);
  }

  get visibleItems(): RankingItem[] {
    return this.items.filter(i => i.category === this.activeCategory);
  }

  get isValueRanked(): boolean {
    return this.visibleItems.some(i => i.valueScore != null);
  }

  productLink(item: RankingItem): string[] {
    return ['/product', String(item.catalogProductId), this.slugify(item.modelLabel)];
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
