import { Component } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-contact',
  standalone: true,
  templateUrl: './contact.html'
})
export class Contact {
  constructor(title: Title) {
    title.setTitle('Contact DealFinder');
  }
}
