import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ProductResult } from '../models/product-result';

@Injectable({
  providedIn: 'root'
})
export class ProductSearchService {
  private readonly apiUrl = 'http://localhost:5185/api/ProductSearch/search';

  constructor(private http: HttpClient) {}

  search(catalogProductId: number): Observable<ProductResult[]> {
    return this.http.post<ProductResult[]>(this.apiUrl, {
      catalogProductId
    });
  }
}