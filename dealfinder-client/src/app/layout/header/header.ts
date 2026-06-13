import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './header.html'
})
export class Header implements OnInit {
  menuOpen = false;
  isDark = false;

  constructor(private router: Router) {}

  ngOnInit(): void {
    const stored = localStorage.getItem('df-theme');
    this.isDark = stored === 'dark';
    this.applyTheme();
  }

  toggle(): void {
    this.menuOpen = !this.menuOpen;
  }

  close(): void {
    this.menuOpen = false;
  }

  toggleTheme(): void {
    this.isDark = !this.isDark;
    localStorage.setItem('df-theme', this.isDark ? 'dark' : 'light');
    this.applyTheme();
  }

  searchNavigate(event: Event): void {
    const input = event.target as HTMLInputElement;
    const q = input.value.trim();
    if (q) {
      this.router.navigate(['/marketplace'], { queryParams: { q } });
      input.value = '';
    }
  }

  private applyTheme(): void {
    const html = document.documentElement;
    if (this.isDark) {
      html.classList.add('dark');
    } else {
      html.classList.remove('dark');
    }
  }
}
