import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-rankings',
  standalone: true,
  templateUrl: './rankings.html'
})
export class Rankings {
  constructor(title: Title) {
    title.setTitle('How Rankings Work | DealFinder');
  }
}
