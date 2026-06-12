import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-about',
  standalone: true,
  templateUrl: './about.html'
})
export class About {
  constructor(title: Title) {
    title.setTitle('About DealFinder | AI PC Hardware Price Intelligence');
  }
}
