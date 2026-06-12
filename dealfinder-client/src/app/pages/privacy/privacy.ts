import { Component } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';

@Component({
  selector: 'app-privacy',
  standalone: true,
  templateUrl: './privacy.html'
})
export class Privacy {
  constructor(title: Title, meta: Meta) {
    const t = 'Privacy Policy | DealFinder';
    const d = 'How DealFinder collects, uses, and protects your data.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }
}
