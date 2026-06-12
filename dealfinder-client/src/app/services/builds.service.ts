import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PcBuildResult } from '../models/pc-build';

export interface SaveBuildRequest {
  budget: number;
  totalPrice: number;
  aiSummary: string;
  parts: PcBuildResult['parts'];
}

export interface SaveBuildResponse { id: string; }

@Injectable({ providedIn: 'root' })
export class BuildsService {
  constructor(private http: HttpClient) {}

  saveBuild(req: SaveBuildRequest): Observable<SaveBuildResponse> {
    return this.http.post<SaveBuildResponse>(`${environment.apiBaseUrl}/api/builds`, req);
  }
}
