import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';


interface CryptoPurchase {
  cryptoName: string;
  amount: number;
  currency: string;
  date: Date;
  pricePerUnit: number;
  exchangeName: string;
}

@Component({
  selector: 'app-create-crypto',
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
    <div class="container">
      <h1 matDialogTitle>New crypto tracker</h1>

      <div class="content">
        <mat-form-field appearance="outline">
          <mat-label>Pick a crypto</mat-label>
          <mat-select  name="selectedCrypto">
            <mat-option *ngFor="let option of cryptoOptions" [value]="option">
              {{ option }}
            </mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Select currency of purchase</mat-label>
          <mat-select  name="selectedCrypto">
            <mat-option *ngFor="let option of currenciesOptions" [value]="option">
              {{ option }}
            </mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      <div class="actions">
        <button mat-raised-button color="warn" (click)="cancel()">Cancel</button>
        <button mat-raised-button (click)="save()" color="primary">Save</button>
      </div>
    </div>
  `,
  styles: [`
    mat-form-field{
      width: 100%;
    }
    button{
      margin-left:10px;
    }
    .container{
      display:flex;
      flex-direction:column;
      padding:20px;
    }
  `]
})
export class CreateCryptoComponent {
  private dialogRef = inject(MatDialogRef<CreateCryptoComponent>);

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
  currenciesOptions: any[] = [
    'BRL',
    'USD',
  ]
  cancel() {
    this.dialogRef.close()
  }
  save() { }
}
