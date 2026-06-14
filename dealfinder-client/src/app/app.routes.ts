import { Routes } from '@angular/router';

import { Home } from './pages/home/home';
import { Marketplace } from './pages/marketplace/marketplace';
import { About } from './pages/about/about';
import { AlertsPage } from './pages/alerts/alerts';
import { Rankings } from './pages/rankings/rankings';
import { Privacy } from './pages/privacy/privacy';
import { Contact } from './pages/contact/contact';
import { Terms } from './pages/terms/terms';
import { Sitemap } from './pages/sitemap/sitemap';
import { PcAssembler } from './pages/pc-assembler/pc-assembler';
import { NotFound } from './pages/not-found/not-found';
import { BuildShare } from './pages/build-share/build-share';
import { ProductPage } from './pages/product/product';
import { BlogList } from './pages/blog/blog-list';
import { BlogPostPage } from './pages/blog/blog-post';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'marketplace', component: Marketplace },
  { path: 'pc-assembler', component: PcAssembler },
  { path: 'about', component: About },
  { path: 'alerts', component: AlertsPage },
  { path: 'rankings', component: Rankings },
  { path: 'rankings/:category', component: Rankings },
  { path: 'product/:id', component: ProductPage },
  { path: 'product/:id/:slug', component: ProductPage },
  { path: 'blog', component: BlogList },
  { path: 'blog/:slug', component: BlogPostPage },
  { path: 'privacy', component: Privacy },
  { path: 'contact', component: Contact },
  { path: 'terms', component: Terms },
  { path: 'sitemap', component: Sitemap },
  { path: 'build/:id', component: BuildShare },
  { path: '**', component: NotFound }
];