import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component } from '@angular/core';

import { HeaderComponent } from './header.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    HeaderComponent,
    HttpClientModule],
  template: `
    <div class="container">
      <img src="assets/bitcoin_illustration2.png" alt="bitcoin illustration image" />
    </div>
  `,
  styles: [
    `
      .container{
        display: flex;
        justify-content: center;
        align-items: center;
        height:calc(80vh + 20px);
      }
      img {
      width: 43.75rem;
      }
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
