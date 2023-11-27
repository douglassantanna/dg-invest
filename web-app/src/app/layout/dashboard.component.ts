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
      <div class="row">
        <div class="col-lg-6 offset-lg-3 text-center">
          <img src="assets/bitcoin_illustration2.png" alt="Bitcoin illustration image" class="img-fluid">
        </div>
      </div>
    </div>
  `,
  styles: [
    ``,
  ],
})
export class DashboardComponent {

}
