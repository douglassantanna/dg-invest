import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CryptoX } from './interfaces/crypto.model';
import { ViewMinimalCryptoAssetDto } from './services/crypto.service';

@Component({
  selector: 'app-crypto-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="row row-cols-1 row-cols-md-2 g-4">
      <div class="col" *ngFor="let crypto of cryptos">
        <div class="card shadow p-3 mb-5 bg-white rounded">
          <div class="card-body">
            <h2 class="card-title">{{ crypto.symbol }}</h2>
           <!--  <h4 class="card-subtitle mb-2 text-muted">{{ crypto.symbol }}</h4> -->
            <p class="card-text">Price: {{ crypto.currentPrice}}</p>
            <!-- <p class="card-text">Average Price: {{ crypto.averagePrice }}</p>
            <p class="card-text">Price Difference: {{ crypto.priceDifferencePercent | number: '1.2-2' }}%</p> -->
            <a (click)="cryptoDashboard()" class="btn btn-primary">Ver mais</a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class CryptoCardComponent {
  @Input() cryptos: ViewMinimalCryptoAssetDto[] = [];
  cryptoDashboard() {
    console.log('Crypto Dashboard');
  }
}
