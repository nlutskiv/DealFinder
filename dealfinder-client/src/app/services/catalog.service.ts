import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CatalogOption } from '../models/catalog-option';

@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private readonly apiUrl = 'http://localhost:5185/api/Catalog';

  constructor(private http: HttpClient) {}

  getCatalog(): Observable<CatalogOption[]> {
    return this.http.get<CatalogOption[]>(this.apiUrl);
  }
}