import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-terms',
  standalone: true,
  templateUrl: './terms.html'
})
export class Terms {
  constructor(title: Title) {
    title.setTitle('Terms of Use | DealFinder');
  }
}
