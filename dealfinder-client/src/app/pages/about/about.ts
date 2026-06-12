import { Component } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

@Component({
  selector: 'app-about',
  standalone: true,
  templateUrl: './about.html'
})
export class About {
  constructor(title: Title, meta: Meta) {
    const t = 'About DealFinder | AI PC Hardware Price Intelligence';
    const d = 'DealFinder tracks PC component prices across UK retailers, scores deals with AI, and alerts you when prices drop.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }
}
