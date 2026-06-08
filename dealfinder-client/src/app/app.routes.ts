import { Routes } from '@angular/router';

import { Marketplace } from './pages/marketplace/marketplace';
import { About } from './pages/about/about';
import { Rankings } from './pages/rankings/rankings';
import { Privacy } from './pages/privacy/privacy';
import { Contact } from './pages/contact/contact';
import { Terms } from './pages/terms/terms';
import { Sitemap } from './pages/sitemap/sitemap';

export const routes: Routes = [
  {
    path: '',
    component: Marketplace
  },
  {
    path: 'about',
    component: About
  },
  {
    path: 'rankings',
    component: Rankings
  },
  {
    path: 'privacy',
    component: Privacy
  },
  {
    path: 'contact',
    component: Contact
  },
  {
    path: 'terms',
    component: Terms
  },
  {
    path: 'sitemap',
    component: Sitemap
  },
  {
    path: '**',
    redirectTo: ''
  }
];