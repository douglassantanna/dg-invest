import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';

import { CryptoX } from './interfaces/crypto.model';
import { CreateCryptoComponent } from './create-crypto.component';
import { AddTransactionComponent } from './add-transaction.component';
import { CryptoCardComponent } from './crypto-card.component';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    FormsModule,
    MatSelectModule,
    MatDialogModule,
    CryptoCardComponent],
  template: `
    <main class="main-container">
      <header>
        <h1>Portfolio</h1>
        <div class="filters">

      <button
        type="button"
        mat-raised-button
        color="primary"
        (click)="createCrypto()">
        <mat-icon>add</mat-icon> Add Crypto
      </button>

        </div>

      </header>

      <div>

      <div class="row g-3">
        <div class="col-sm">
          <input type="text" class="form-control" placeholder="Search by name.." aria-label="Search">
        </div>
        <div class="col-sm">
          <input type="text" class="form-control" placeholder="Zip" aria-label="Zip">
        </div>
      </div>

      </div>

      <app-crypto-card [cryptos]="cryptos" />
    </main>
  `,
  styles: [`
    .main-container {
        display: flex;
        flex-direction: column;
        padding:20px;
      }
    header{
      display: flex;
      justify-content:space-between;
      align-items:center;
      gap:10px;
    }
    .filters{
      padding:5px;
    }
    .crypto-container {
      display: grid;
        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
        grid-gap: 1rem;
        margin-bottom: 1rem;
    }
    mat-form-field{
      width:20vw;
    }
    @media (max-width: 640px) {
        .crypto-container {
          grid-template-columns: 1fr;
          justify-content: center;
          align-items: center;
        }
        header{
          flex-direction:column;
          align-items:center;
        }
        mat-form-field{
          width:100%;
        }
      }
  `]
})
export class ViewCryptosComponent {
  private router = inject(Router);
  private dialog = inject(MatDialog);
  orderOptions: any[] = [
    'ASC',
    'DESC'
  ];
  dateOptions: any[] = [
    '1 dia',
    '7 dias',
    '1 mês',
    '3 meses',
    '6 meses',
    '1 ano'
  ]
  cryptos: CryptoX[] = [
    {
      name: 'Bitcoin',
      symbol: 'BTC',
      price: 47000,
      averagePrice: 45000,
      priceDifferencePercent: 4.44,
    },
    {
      name: 'Ethereum',
      symbol: 'ETH',
      price: 500,
      averagePrice: 100,
      priceDifferencePercent: 2.24,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
    {
      name: 'Chain Link',
      symbol: 'LINK',
      price: 157,
      averagePrice: 124,
      priceDifferencePercent: 1.47,
    },
  ];

  searchValue = '';
  constructor() {
    this.calculateCryptoValues();
  }
  createCrypto(): void {
    const dialogRef = this.dialog.open(CreateCryptoComponent, {
      width: '400px',
      height: '300px',
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('The dialog was closed');
      console.log(result);
    }
    );
  }
  cryptoDashboard() {
    this.router.navigate(['/crypto-dashboard', 1]);
  }
  search(event: any) { }
  private calculateCryptoValues(): void {
    const purchasedPrices: number[] = [46000, 48000, 44000];

    this.cryptos.forEach((crypto) => {
      const sumOfPurchasedPrices = purchasedPrices.reduce((acc, price) => acc + price, 0);
      const averagePrice = sumOfPurchasedPrices / purchasedPrices.length;
      const priceDifference = crypto.price - averagePrice;
      const priceDifferencePercent = (priceDifference / averagePrice) * 100;

      crypto.averagePrice = averagePrice;
      crypto.priceDifferencePercent = priceDifferencePercent;
    });
  }
}
