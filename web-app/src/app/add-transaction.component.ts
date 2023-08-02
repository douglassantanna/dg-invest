import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { PurchaseHistoryComponent } from './purchase-history.component';
import { MyCryptoComponent } from './my-crypto.component';

@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    FormsModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    PurchaseHistoryComponent,
    MyCryptoComponent],
  template: `
  <div class="grid-container">
    <div class="div1">
    <app-my-crypto />
    </div>
      <div class="div2">
      <mat-card>
        <h1>{{title}}</h1>
        <mat-card-content>
          <div class="fields">
            <mat-form-field appearance="outline">
              <mat-label>Amount</mat-label>
              <input matInput type="number"  name="amount">
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Price per Unit</mat-label>
              <input matInput type="number"  name="pricePerUnit">
            </mat-form-field>
          </div>

          <div class="fields">
            <mat-form-field appearance="outline">
              <mat-label>Date of Purchase</mat-label>
              <input matInput [matDatepicker]="picker">
              <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
              <mat-datepicker #picker></mat-datepicker>
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Exchange Name</mat-label>
              <input matInput type="text" name="exchangeName">
            </mat-form-field>
          </div>

          <div class="fields">
            <mat-form-field appearance="outline">
              <mat-label>Transcation type</mat-label>
              <mat-select>
                <mat-option [value]="'1'">Buy</mat-option>
                <mat-option [value]="'1'">Sell</mat-option>
              </mat-select>
            </mat-form-field>
          </div>
        </mat-card-content>
        <mat-card-actions align="end">
          <button mat-raised-button color="warn">Reset</button>
          <button mat-raised-button (click)="save()" color="primary">Save</button>
        </mat-card-actions>
      </mat-card>
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
    mat-form-field{
      width: 100%;
    }
    .fields{
      display:flex;
      gap:10px;
    }
    button{
      margin-left:10px;
    }
    .row {
      display: flex;
      flex-direction: row;
      align-items: center;
    }
    .col {
      flex: 1;
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
        .fields{
          display:flex;
          flex-direction:column;
          gap:10px;
        }
      }
  `]
})
export class AddTransactionComponent {
  title = 'Add transaction';
  cryptoOptions: any[] = [
    'Bitcoin',
    'Ethereum',
    'Tether',
    'Litecoin',
    'Cardano',
    'Binance Coin',
    'Polkadot',
    'Solana',
    'Avalanche',
  ];
  save() { }
}
