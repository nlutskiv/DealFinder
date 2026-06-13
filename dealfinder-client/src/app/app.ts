import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { Header } from './layout/header/header';
import { Footer } from './layout/footer/footer';
import { CookieConsent } from './layout/cookie-consent/cookie-consent';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Header, Footer, CookieConsent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private platformId = inject(PLATFORM_ID);
  private document = inject(DOCUMENT);
  private router = inject(Router);

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.setCanonical(this.router.url);

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(e => this.setCanonical(e.urlAfterRedirects));
  }

  private setCanonical(url: string): void {
    const path = url.split('?')[0].split('#')[0] || '/';
    let link = this.document.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', `https://www.dealfinder.tech${path}`);
  }
}
