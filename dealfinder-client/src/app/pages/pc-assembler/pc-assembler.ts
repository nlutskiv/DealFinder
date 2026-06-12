import { Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Meta, Title } from '@angular/platform-browser';

import { PcBuildService } from '../../services/pc-build.service';
import { BuildsService } from '../../services/builds.service';
import { PcBuildResult, PcBuildPart } from '../../models/pc-build';

const CATEGORY_ORDER = ['gpu', 'cpu', 'motherboard', 'ram', 'storage', 'psu', 'case', 'cooler'];

const CATEGORY_LABELS: Record<string, string> = {
  gpu: 'GPU', cpu: 'CPU', motherboard: 'Motherboard', ram: 'RAM',
  storage: 'Storage', psu: 'PSU', case: 'Case', cooler: 'CPU Cooler',
};

const LOADING_STEPS = [
  'Scanning listings...',
  'Selecting compatible parts...',
  'Running compatibility check...',
  'Finalising your build...',
];

const STEP_DELAYS_MS = [4000, 10000, 20000];

@Component({
  selector: 'app-pc-assembler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pc-assembler.html',
  styleUrl: './pc-assembler.css'
})
export class PcAssembler implements OnDestroy {
  @ViewChild('buildSection') buildSection!: ElementRef;

  budget = 1000;
  loading = false;
  loadingStep = LOADING_STEPS[0];
  error = '';
  result: PcBuildResult | null = null;
  shareId: string | null = null;
  sharing = false;

  private stepTimers: ReturnType<typeof setTimeout>[] = [];

  constructor(
    private pcBuildService: PcBuildService,
    private buildsService: BuildsService,
    title: Title,
    meta: Meta
  ) {
    const t = 'PC Assembler — Build a Full PC from Your Budget | DealFinder';
    const d = 'Enter your budget and get a compatible, best-value PC build picked from live UK retailer listings — instantly.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ property: 'og:url', content: 'https://www.dealfinder.tech/pc-assembler' });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
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

  get missingCategories(): string[] {
    if (!this.result) return [];
    const present = new Set(this.result.parts.map(p => p.category));
    return CATEGORY_ORDER
      .filter(cat => !present.has(cat))
      .map(cat => CATEGORY_LABELS[cat] ?? cat);
  }

  generate(): void {
    if (this.budget < 300) {
      this.error = 'Budget must be at least £300 for a realistic build.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.result = null;
    this.loadingStep = LOADING_STEPS[0];
    this.clearStepTimers();

    STEP_DELAYS_MS.forEach((delay, i) => {
      const t = setTimeout(() => { this.loadingStep = LOADING_STEPS[i + 1]; }, delay);
      this.stepTimers.push(t);
    });

    this.pcBuildService.generateBuild(this.budget).subscribe({
      next: result => {
        this.result = result;
        this.loading = false;
        this.shareId = null;
        this.clearStepTimers();
        setTimeout(() => {
          this.buildSection?.nativeElement?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 80);
      },
      error: err => {
        this.error = err?.error?.message || 'Failed to generate build. Please try again.';
        this.loading = false;
        this.clearStepTimers();
      }
    });
  }

  rebuild(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
    setTimeout(() => this.generate(), 350);
  }

  shareBuild(): void {
    if (!this.result || this.sharing) return;
    this.sharing = true;
    this.buildsService.saveBuild({
      budget: this.result.budget,
      totalPrice: this.result.totalPrice,
      aiSummary: this.result.aiSummary,
      parts: this.result.parts,
    }).subscribe({
      next: resp => {
        this.shareId = resp.id;
        this.sharing = false;
        const url = `${window.location.origin}/build/${resp.id}`;
        navigator.clipboard?.writeText(url).catch(() => {});
      },
      error: () => { this.sharing = false; },
    });
  }

  get shareUrl(): string {
    return this.shareId ? `${window.location.origin}/build/${this.shareId}` : '';
  }

  onImageError(event: Event): void {
    (event.target as HTMLImageElement).src = '/fallback-product.svg';
  }

  ngOnDestroy(): void {
    this.clearStepTimers();
  }

  private clearStepTimers(): void {
    this.stepTimers.forEach(t => clearTimeout(t));
    this.stepTimers = [];
  }
}
