import { Component, OnInit, OnDestroy, Inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule, DOCUMENT } from '@angular/common';
import { Meta, Title } from '@angular/platform-browser';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { BLOG_POSTS, BlogPost } from './posts';

@Component({
  selector: 'app-blog-post',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './blog-post.html',
  styleUrl: './blog.css',
})
export class BlogPostPage implements OnInit, OnDestroy {
  post: BlogPost | null = null;
  safeBody: SafeHtml = '';
  notFound = false;

  constructor(
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer,
    private titleService: Title,
    private meta: Meta,
    @Inject(DOCUMENT) private document: Document,
  ) {}

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    this.post = BLOG_POSTS.find(p => p.slug === slug) ?? null;
    if (!this.post) {
      this.notFound = true;
      return;
    }
    this.safeBody = this.sanitizer.bypassSecurityTrustHtml(this.post.body);

    const t = `${this.post.title} | DealFinder Blog`;
    const url = `https://www.dealfinder.tech/blog/${this.post.slug}`;

    this.titleService.setTitle(t);
    this.meta.updateTag({ name: 'description', content: this.post.excerpt });
    this.meta.updateTag({ property: 'og:title', content: t });
    this.meta.updateTag({ property: 'og:description', content: this.post.excerpt });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:type', content: 'article' });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary' });
    this.meta.updateTag({ name: 'twitter:title', content: t });
    this.meta.updateTag({ name: 'twitter:description', content: this.post.excerpt });

    this.injectStructuredData({
      '@context': 'https://schema.org',
      '@type': 'Article',
      headline: this.post.title,
      description: this.post.excerpt,
      datePublished: this.post.date,
      publisher: {
        '@type': 'Organization',
        name: 'DealFinder',
        url: 'https://www.dealfinder.tech',
      },
      mainEntityOfPage: {
        '@type': 'WebPage',
        '@id': url,
      },
    });
  }

  ngOnDestroy(): void {
    this.document.getElementById('blog-ld-json')?.remove();
  }

  private injectStructuredData(data: object): void {
    this.document.getElementById('blog-ld-json')?.remove();
    const script = this.document.createElement('script');
    script.id = 'blog-ld-json';
    script.type = 'application/ld+json';
    script.text = JSON.stringify(data);
    this.document.head.appendChild(script);
  }
}
