import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { StatsService, SiteStats } from '../../services/stats.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  private platformId = inject(PLATFORM_ID);

  stats: SiteStats | null = null;

  constructor(private statsService: StatsService, title: Title, meta: Meta) {
    const t = 'DealFinder — Find PC Hardware Deals You Can Trust';
    const d = 'Compare PC parts prices across UK retailers. AI deal scoring, 30-day price history, and price drop alerts — all free.';
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
  }

  get updatedAgo(): string | null {
    if (!this.stats?.lastUpdatedUtc) return null;
    const hours = Math.max(1, Math.round(
      (Date.now() - new Date(this.stats.lastUpdatedUtc).getTime()) / 3600000
    ));
    return hours < 24 ? `${hours}h ago` : `${Math.round(hours / 24)}d ago`;
  }
}
