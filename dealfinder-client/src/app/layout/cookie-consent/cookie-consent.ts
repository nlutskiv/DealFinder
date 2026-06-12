import { Component, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

const CONSENT_KEY = 'df_cookie_consent';

@Component({
  selector: 'app-cookie-consent',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './cookie-consent.html',
  styleUrl: './cookie-consent.css'
})
export class CookieConsent implements OnInit {
  private platformId = inject(PLATFORM_ID);
  visible = false;

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.visible = !localStorage.getItem(CONSENT_KEY);
    }
  }

  accept(): void {
    localStorage.setItem(CONSENT_KEY, '1');
    this.visible = false;
  }
}
