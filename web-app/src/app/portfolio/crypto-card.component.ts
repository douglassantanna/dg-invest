import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ViewMinimalCryptoAssetDto } from '../core/services/crypto.service';
import { DecimalRoundPipe } from '../core/pipes/decimal-round.pipe';
import { Router } from '@angular/router';

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
            <a (click)="cryptoDashboard(crypto.id)" class="btn btn-primary">See details</a>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class CryptoCardComponent {
  @Input() cryptos: ViewMinimalCryptoAssetDto[] = [];

  private router = inject(Router);

  cryptoDashboard(cryptoID: number) {
    this.router.navigate(['/crypto-dashboard', cryptoID]);
  }
}
