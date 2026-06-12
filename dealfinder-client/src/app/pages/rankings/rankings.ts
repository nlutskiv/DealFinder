import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
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

  constructor(private rankingsService: RankingsService, title: Title) {
    title.setTitle('Best PC Part Deals Today | DealFinder');
  }

  ngOnInit(): void {
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
