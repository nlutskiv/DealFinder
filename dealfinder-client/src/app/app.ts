import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductResult } from './models/product-result';

type SortOption = 'score' | 'price-low' | 'price-high';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  query = '';
  results: ProductResult[] = [];
  loading = false;
  error = '';
  sortBy: SortOption = 'score';

  constructor(private http: HttpClient) {}

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

  search() {
    if (!this.query.trim()) {
      this.error = 'Please enter a product name.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.results = [];

    this.http
      .post<ProductResult[]>('http://localhost:5185/api/ProductSearch/search', {
        query: this.query
      })
      .subscribe({
        next: (data) => {
          this.results = data;
          this.loading = false;
        },
        error: (err) => {
          console.error(err);
          this.error = 'Search failed. Is the backend running?';
          this.loading = false;
        }
      });
  }
}