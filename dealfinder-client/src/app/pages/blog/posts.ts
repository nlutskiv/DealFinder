export interface BlogPost {
  slug: string;
  title: string;
  date: string;
  excerpt: string;
  category: string;
  readTime: number;
  body: string;
}

export const BLOG_POSTS: BlogPost[] = [];
