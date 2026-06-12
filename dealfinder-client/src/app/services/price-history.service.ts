import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PricePoint { date: string; minPrice: number; }

@Injectable({ providedIn: 'root' })
export class PriceHistoryService {
  constructor(private http: HttpClient) {}

  getHistory(catalogId: number): Observable<PricePoint[]> {
    return this.http.get<PricePoint[]>(
      `${environment.apiBaseUrl}/api/PriceHistory/${catalogId}`
    );
  }
}
