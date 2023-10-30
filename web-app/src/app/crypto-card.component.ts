import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CryptoX } from './interfaces/crypto.model';
import { ViewMinimalCryptoAssetDto } from './services/crypto.service';
import { DecimalRoundPipe } from './pipes/decimal-round.pipe';

@Component({
  selector: 'app-crypto-card',
  standalone: true,
  imports: [CommonModule, DecimalRoundPipe],
  template: `
    <div class="row row-cols-1 row-cols-md-2 g-4">
      <div class="col" *ngFor="let crypto of cryptos">
        <div class="card shadow p-3 mb-5 bg-white rounded">
          <div class="card-body">
            <h2 class="card-title">{{ crypto.symbol | uppercase }}</h2>
            <p class="card-text">Price: {{ crypto.currentPrice | currency:'USD' }}</p>
            <p class="card-text">Percent Change 24h:
              <span [ngStyle]="{ color: crypto.percentChange24h >= 0 ? 'green' : 'red', 'font-weight': 'bold' }">
                {{ crypto.percentChange24h | decimalRound }}%
              </span>
            </p>
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
