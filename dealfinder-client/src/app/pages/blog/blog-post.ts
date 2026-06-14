import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
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
export class BlogPostPage implements OnInit {
  post: BlogPost | null = null;
  safeBody: SafeHtml = '';
  notFound = false;

  constructor(
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer,
    private titleService: Title,
    private meta: Meta,
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
    this.titleService.setTitle(t);
    this.meta.updateTag({ name: 'description', content: this.post.excerpt });
    this.meta.updateTag({ property: 'og:title', content: t });
    this.meta.updateTag({ property: 'og:description', content: this.post.excerpt });
    this.meta.updateTag({ name: 'twitter:title', content: t });
    this.meta.updateTag({ name: 'twitter:description', content: this.post.excerpt });
  }
}
