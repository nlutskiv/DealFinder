import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';

import { PcBuildService } from '../../services/pc-build.service';
import { PcBuildResult, PcBuildPart } from '../../models/pc-build';

const CATEGORY_ORDER = ['gpu', 'cpu', 'motherboard', 'ram', 'storage', 'psu', 'case', 'cooler'];

@Component({
  selector: 'app-pc-assembler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pc-assembler.html',
  styleUrl: './pc-assembler.css'
})
export class PcAssembler {
  budget: number = 1000;
  loading = false;
  error = '';
  result: PcBuildResult | null = null;

  constructor(private pcBuildService: PcBuildService, title: Title) {
    title.setTitle('PC Assembler — Build a Full PC from Your Budget | DealFinder');
  }

  get sortedParts(): PcBuildPart[] {
    if (!this.result) return [];
    return [...this.result.parts].sort(
      (a, b) => CATEGORY_ORDER.indexOf(a.category) - CATEGORY_ORDER.indexOf(b.category)
    );
  }

  get savingsVsBudget(): number {
    return this.result ? this.result.budget - this.result.totalPrice : 0;
  }

  get hasWarnings(): boolean {
    return (this.result?.compatibilityWarnings?.length ?? 0) > 0;
  }

  generate(): void {
    if (this.budget < 150) {
      this.error = 'Budget must be at least £150.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.result = null;

    this.pcBuildService.generateBuild(this.budget).subscribe({
      next: result => {
        this.result = result;
        this.loading = false;
      },
      error: err => {
        this.error = err?.error?.message || 'Failed to generate build. Please try again.';
        this.loading = false;
      }
    });
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
  }
}
