import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { HeaderComponent } from './header.component';
import { HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    HeaderComponent,
    HttpClientModule],
  template: `
    <app-header />
    <router-outlet></router-outlet>
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
export class DashboardComponent {

}
