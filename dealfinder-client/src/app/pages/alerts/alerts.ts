import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { AlertService, AlertDto } from '../../services/alert.service';

@Component({
  selector: 'app-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './alerts.html',
  styleUrl: './alerts.css'
})
export class AlertsPage {
  email = '';
  loading = false;
  looked = false;
  alerts: AlertDto[] = [];
  error = '';
  deletingId: number | null = null;

  constructor(title: Title, meta: Meta, private alertService: AlertService) {
    const t = 'Price Alerts | DealFinder';
    const d = 'Manage your PC component price drop alerts. Get notified when a GPU, CPU, or other part drops below your target price.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
  }

  lookup(): void {
    if (!this.email || !this.email.includes('@')) return;
    this.loading = true;
    this.error = '';
    this.looked = false;
    this.alertService.getAlerts(this.email).subscribe({
      next: (data) => {
        this.alerts = data;
        this.looked = true;
        this.loading = false;
      },
      error: () => {
        this.error = 'Could not load alerts. Please try again.';
        this.loading = false;
      }
    });
  }

  delete(id: number): void {
    this.deletingId = id;
    this.alertService.deleteAlert(id, this.email).subscribe({
      next: () => {
        this.alerts = this.alerts.filter(a => a.id !== id);
        this.deletingId = null;
      },
      error: () => {
        this.deletingId = null;
      }
    });
  }

  categoryLabel(cat: string): string {
    const map: Record<string, string> = {
      gpu: 'GPU', cpu: 'CPU', ram: 'RAM', psu: 'PSU',
      motherboard: 'Motherboard', storage: 'Storage', cooler: 'Cooler', case: 'Case'
    };
    return map[cat] ?? cat.toUpperCase();
  }
}
