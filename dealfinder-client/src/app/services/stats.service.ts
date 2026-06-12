import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SiteStats {
  listingCount: number;
  modelCount: number;
  retailerCount: number;
  lastUpdatedUtc: string | null;
}

@Injectable({ providedIn: 'root' })
export class StatsService {
  private readonly apiUrl = `${environment.apiBaseUrl}/api/Stats`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<SiteStats> {
    return this.http.get<SiteStats>(this.apiUrl);
  }
}
