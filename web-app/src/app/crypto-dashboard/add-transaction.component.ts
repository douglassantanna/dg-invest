import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule],
  template: `
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
  `,
  styles: [`
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
    @media (max-width: 640px) {
        .fields{
          display:flex;
          flex-direction:column;
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
