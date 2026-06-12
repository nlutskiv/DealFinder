import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface RankingItem {
  category: string;
  categoryLabel: string;
  modelLabel: string;
  catalogProductId: number;
  minPrice: number;
  listingCount: number;
  topListingUrl: string;
  topListingImageUrl: string;
  topListingMarketplace: string;
}

@Injectable({ providedIn: 'root' })
export class RankingsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Rankings`;

  constructor(private http: HttpClient) {}

  getRankings(): Observable<RankingItem[]> {
    return this.http.get<RankingItem[]>(this.apiUrl);
  }
}
