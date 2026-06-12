import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { PcBuildResult } from '../../models/pc-build';

@Component({
  selector: 'app-build-share',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './build-share.html',
  styleUrl: './build-share.css'
})
export class BuildShare implements OnInit {
  result: PcBuildResult | null = null;
  loading = true;
  error = '';

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
    title: Title
  ) {
    title.setTitle('Shared PC Build | DealFinder');
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.error = 'Invalid build link.'; this.loading = false; return; }

    this.http.get<PcBuildResult>(`${environment.apiBaseUrl}/api/builds/${id}`).subscribe({
      next: result => { this.result = result; this.loading = false; },
      error: () => { this.error = 'Build not found or has expired.'; this.loading = false; }
    });
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
  }
}
