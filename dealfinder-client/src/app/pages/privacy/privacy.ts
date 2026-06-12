import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-privacy',
  standalone: true,
  templateUrl: './privacy.html'
})
export class Privacy {
  constructor(title: Title) {
    title.setTitle('Privacy Policy | DealFinder');
  }
}
