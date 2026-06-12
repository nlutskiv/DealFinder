import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  visible = false;

  ngOnInit(): void {
    this.visible = !localStorage.getItem(CONSENT_KEY);
  }

  accept(): void {
    localStorage.setItem(CONSENT_KEY, '1');
    this.visible = false;
  }
}
