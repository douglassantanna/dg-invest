import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

import { AddTransactionComponent } from './add-transaction.component';
import { MyCryptoComponent } from './my-crypto.component';
import { PurchaseHistoryComponent } from './purchase-history.component';

@Component({
  selector: 'app-crypto-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    AddTransactionComponent,
    MyCryptoComponent,
    PurchaseHistoryComponent,
    MatCardModule],
  template: `
  <div class="grid-container">
    <div class="div1">
      <app-my-crypto />
    </div>
    <div class="div2">
      <app-add-transaction/>
    </div>
    <div class="div3">
      <mat-card>
        <h1>Purchase history</h1>
        <mat-card-content>
          <app-purchase-history />
        </mat-card-content>
      </mat-card>
    </div>
  </div>
  `,
  styles: [`
    .grid-container {
      display: grid;
      grid-template-columns: 1fr 1fr;
      grid-template-rows: 0.4fr 1.6fr;
      gap: 10px 10px;
      grid-template-areas:
        "div1 div3"
        "div2 div3";
      padding:10px;
    }
    .div1 {
      grid-area:div1;
    }
    .div2 {
      grid-area:div2;
    }
    .div3 {
      grid-area:div3;
      max-height:600px;
      overflow:auto;
    }
    h1{
      padding:16px 0px 0px 16px
    }
    @media (max-width: 640px) {
        .grid-container {
          grid-template-columns: 1fr;
          grid-template-rows: 0.4fr;
          grid-template-areas:
          "div1"
          "div2"
          "div3";
          justify-content: center;
          align-items: center;
          gap: 5px;
        }
      }
  `]
})
export class CryptoDashboardComponent {

}
