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
    MatNativeDateModule],
  template: `
  <mat-card>
  <mat-card-header>
    <mat-card-title>{{ title }}</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    <mat-form-field appearance="outline">
      <mat-label>Pick a crypto</mat-label>
      <mat-select  name="selectedCrypto">
        <mat-option *ngFor="let option of cryptoOptions" [value]="option">
          {{ option }}
        </mat-option>
      </mat-select>
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Amount</mat-label>
      <input matInput type="number"  name="amount">
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Currency</mat-label>
      <input matInput  name="currency">
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Date of Purchase</mat-label>
      <input matInput [matDatepicker]="picker"  name="date">
      <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
      <mat-datepicker #picker></mat-datepicker>
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Price per Unit</mat-label>
      <input matInput type="number"  name="pricePerUnit">
    </mat-form-field>

    <mat-form-field appearance="outline">
      <mat-label>Exchange Name</mat-label>
      <input matInput type="text" name="exchangeName">
    </mat-form-field>
  </mat-card-content>
  <mat-card-actions align="end">
    <button mat-raised-button color="accent">Cancel</button>
    <button mat-raised-button (click)="save()" color="primary">Save</button>
  </mat-card-actions>
  <mat-card-footer>
    Footer
  </mat-card-footer>
</mat-card>

  `,
  styles: [`
    mat-card{
      display:flex;
      flex-direction: column;
      width: 400px;
      height: 100%;
    }
    mat-form-field{
      width: 100%;
    }
    button{
      margin-left:10px;
    }
  `]
})
export class AddTransactionComponent {
  title = 'New Crypto';
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
