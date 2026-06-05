import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

interface ProductResult {
  title: string;
  description: string;
  price: number;
  shippingPrice: number;
  condition: string;
  marketplace: string;
  url: string;
  score: number;
  summary: string;
}

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

  constructor(private http: HttpClient) {}

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