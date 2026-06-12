import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { RankingsService, RankingItem } from '../../services/rankings.service';

const CATEGORY_ORDER = ['gpu', 'cpu', 'motherboard', 'ram', 'storage', 'psu', 'case', 'cooler'];

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

  constructor(private rankingsService: RankingsService, title: Title, meta: Meta) {
    const t = 'Best PC Part Deals Today | DealFinder';
    const d = "Today's cheapest GPUs, CPUs, RAM, storage, and more across UK retailers — updated daily.";
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ property: 'og:url', content: 'https://www.dealfinder.tech/rankings' });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.rankingsService.getRankings().subscribe({
      next: items => {
        this.items = [...items].sort(
          (a, b) => CATEGORY_ORDER.indexOf(a.category) - CATEGORY_ORDER.indexOf(b.category)
        );
        this.loading = false;
        if (this.items.length > 0) this.activeCategory = this.items[0].category;
      },
      error: () => { this.error = 'Could not load rankings.'; this.loading = false; }
    });
  }

  get visibleItems(): RankingItem[] {
    return this.items.filter(i => i.category === this.activeCategory);
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
  }
}
