import { Component } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

@Component({
  selector: 'app-about',
  standalone: true,
  templateUrl: './about.html'
})
export class About {
  constructor(title: Title, meta: Meta) {
    const t = 'About DealFinder | UK PC Hardware Price Tracking';
    const d = 'DealFinder tracks PC component prices across UK retailers, with 30-day price history and alerts when prices drop.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }
}
