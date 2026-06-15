import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Meta, Title } from '@angular/platform-browser';
import { BLOG_POSTS, BlogPost } from './posts';

@Component({
  selector: 'app-blog-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './blog-list.html',
  styleUrl: './blog.css',
})
export class BlogList {
  posts: BlogPost[] = BLOG_POSTS;

  constructor(title: Title, meta: Meta) {
    const t = 'Blog | DealFinder';
    const d = 'PC hardware buying guides, price trend analysis, and tips for getting the best deal on UK PC parts.';
    title.setTitle(t);
    meta.updateTag({ name: 'description', content: d });
    meta.updateTag({ property: 'og:title', content: t });
    meta.updateTag({ property: 'og:description', content: d });
    meta.updateTag({ property: 'og:url', content: 'https://www.dealfinder.tech/blog' });
    meta.updateTag({ property: 'og:type', content: 'website' });
    meta.updateTag({ name: 'twitter:card', content: 'summary' });
    meta.updateTag({ name: 'twitter:title', content: t });
    meta.updateTag({ name: 'twitter:description', content: d });
  }
}
