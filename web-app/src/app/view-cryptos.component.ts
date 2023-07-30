import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

import { CryptoX } from './interfaces/crypto.model';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="crypto-container">
      <mat-card *ngFor="let crypto of cryptos" class="crypto-card">
        <mat-card-header>
          <mat-card-title>{{ crypto.name }}</mat-card-title>
          <mat-card-subtitle>{{ crypto.symbol }}</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>Price: $ {{ crypto.price }}</p>
          <p>Average Price: $ {{ crypto.averagePrice }}</p>
          <p>Price Difference: {{ crypto.priceDifferencePercent | number: '1.2-2' }}%</p>
        </mat-card-content>
        <mat-card-actions align="end">
          <button mat-button color="accent">Ver mais</button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .crypto-container {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-around;
      padding: 20px;
    }

    .crypto-card {
      width: 300px;
      margin: 10px;
      background-color: #f5f5f5;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .mat-card-header {
      padding: 8px;
      background-color: #2196f3;
      color: white;
    }

    .mat-card-content {
      padding: 8px;
    }

    .mat-card-actions {
      padding: 8px;
    }
  `]
})
export class ViewCryptosComponent {
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
    }
  ];

  constructor() {
    this.calculateCryptoValues();
  }

  private calculateCryptoValues(): void {
    // Assuming you have the purchased prices of each cryptocurrency stored in an array.
    // For simplicity, I'll use static data here. You should replace this with your actual data.
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
