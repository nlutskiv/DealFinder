import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CreateAlertRequest {
  email: string;
  catalogProductId: number;
  targetPrice: number;
}

export interface AlertDto {
  id: number;
  productName: string;
  category: string;
  targetPrice: number;
  createdAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class AlertService {
  constructor(private http: HttpClient) {}

  createAlert(req: CreateAlertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/api/alerts`, req);
  }

  getAlerts(email: string): Observable<AlertDto[]> {
    return this.http.get<AlertDto[]>(`${environment.apiBaseUrl}/api/alerts`, {
      params: { email }
    });
  }

  deleteAlert(id: number, email: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/api/alerts/${id}`, {
      params: { email }
    });
  }
}
