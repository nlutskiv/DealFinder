import { Component, OnInit, OnDestroy, ElementRef, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { StatsService, SiteStats } from '../../services/stats.service';
import { DealsService, HotDeal } from '../../services/deals.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit, OnDestroy {
  private platformId = inject(PLATFORM_ID);
  private el = inject(ElementRef);
  private observer: IntersectionObserver | null = null;

  stats: SiteStats | null = null;
  hotDeals: HotDeal[] = [];

  constructor(
    private statsService: StatsService,
    private dealsService: DealsService,
    title: Title,
    meta: Meta
  ) {
    const t = 'DealFinder — Find PC Hardware Deals You Can Trust';
    const d = 'Compare PC parts prices across UK retailers. 30-day price history and price drop alerts — all free.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ property: 'og:url', content: 'https://www.dealfinder.tech/' });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.statsService.getStats().subscribe({
      next: s => this.stats = s,
      error: () => {},
    });
    this.dealsService.getHotDeals().subscribe({
      next: d => this.hotDeals = d,
      error: () => {},
    });
    this.initScrollReveal();
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  private initScrollReveal(): void {
    this.observer = new IntersectionObserver(
      entries => entries.forEach(e => {
        if (e.isIntersecting) {
          e.target.classList.add('is-visible');
          this.observer!.unobserve(e.target);
        }
      }),
      { threshold: 0.12 }
    );
    setTimeout(() => {
      this.el.nativeElement
        .querySelectorAll('.scroll-section')
        .forEach((s: Element) => this.observer!.observe(s));
    }, 0);
  }

  dealLink(deal: HotDeal): string[] {
    const slug = deal.modelLabel.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
    return ['/product', String(deal.catalogProductId), slug];
  }

  get updatedAgo(): string | null {
    if (!this.stats?.lastUpdatedUtc) return null;
    const hours = Math.max(1, Math.round(
      (Date.now() - new Date(this.stats.lastUpdatedUtc).getTime()) / 3600000
    ));
    return hours < 24 ? `${hours}h ago` : `${Math.round(hours / 24)}d ago`;
  }
}
