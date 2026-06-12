import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface HotDeal {
  catalogProductId: number;
  modelLabel: string;
  category: string;
  currentPrice: number;
  averagePrice: number;
  discountPercent: number;
}

@Injectable({ providedIn: 'root' })
export class DealsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Deals`;

  constructor(private http: HttpClient) {}

  getHotDeals(): Observable<HotDeal[]> {
    return this.http.get<HotDeal[]>(this.apiUrl);
  }
}
