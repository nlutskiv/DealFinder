import { Component } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

@Component({
  selector: 'app-terms',
  standalone: true,
  templateUrl: './terms.html'
})
export class Terms {
  constructor(title: Title, meta: Meta) {
    const t = 'Terms of Use | DealFinder';
    const d = 'DealFinder terms of use — rules for accessing and using the platform.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }
}
