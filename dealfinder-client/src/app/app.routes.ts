import { Routes } from '@angular/router';

import { Home } from './pages/home/home';
import { Marketplace } from './pages/marketplace/marketplace';
import { About } from './pages/about/about';
import { Rankings } from './pages/rankings/rankings';
import { Privacy } from './pages/privacy/privacy';
import { Contact } from './pages/contact/contact';
import { Terms } from './pages/terms/terms';
import { Sitemap } from './pages/sitemap/sitemap';
import { PcAssembler } from './pages/pc-assembler/pc-assembler';
import { NotFound } from './pages/not-found/not-found';
import { BuildShare } from './pages/build-share/build-share';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'marketplace', component: Marketplace },
  { path: 'pc-assembler', component: PcAssembler },
  { path: 'about', component: About },
  { path: 'rankings', component: Rankings },
  { path: 'privacy', component: Privacy },
  { path: 'contact', component: Contact },
  { path: 'terms', component: Terms },
  { path: 'sitemap', component: Sitemap },
  { path: 'build/:id', component: BuildShare },
  { path: '**', component: NotFound }
];