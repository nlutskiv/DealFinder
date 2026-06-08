import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CatalogOption } from '../models/catalog-option';

import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class CatalogService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Catalog`;

  constructor(private http: HttpClient) {}

  getCatalog(): Observable<CatalogOption[]> {
    return this.http.get<CatalogOption[]>(this.apiUrl);
  }
}