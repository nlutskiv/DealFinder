import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateAlertRequest {
  email: string;
  catalogProductId: number;
  targetPrice: number;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
  constructor(private http: HttpClient) {}

  createAlert(req: CreateAlertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/alerts`, req);
  }
}
