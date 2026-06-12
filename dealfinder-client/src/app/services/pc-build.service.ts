import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { PcBuildResult } from '../models/pc-build';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PcBuildService {

  private readonly apiUrl = `${environment.apiBaseUrl}/api/PcBuild/generate`;

  constructor(private http: HttpClient) {}

  generateBuild(budget: number): Observable<PcBuildResult> {
    return this.http.post<PcBuildResult>(this.apiUrl, { budget });
  }
}
