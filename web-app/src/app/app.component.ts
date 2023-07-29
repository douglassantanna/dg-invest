import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent],
  template: `
    <app-header />
  `,
  styles: [
    `
      .toolbar-container {
        display: grid;
        grid-template-columns: 3fr 1fr;
        margin: 24px 0;
      }
      @media (max-width: 640px) {
        .toolbar-container {
          grid-template-columns: 1fr;
          justify-content: center;
          align-items: center;
        }
      }
    `,
  ],
})
export class AppComponent {
  title = 'web-app';
}
