import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ViewMinimalCryptoAssetDto } from 'src/app/core/models/view-minimal-crypto-asset-dto';
import { PercentDifferenceComponent } from './percent-difference.component';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

@Component({
  selector: 'app-crypto-card',
  standalone: true,
  imports: [CommonModule, PercentDifferenceComponent, FormatCurrencyPipe],
  template: `
  <div class="card shadow mb-3 bg-white rounded">
    <div class="card-body">
      <h2 class="card-title">{{ crypto.symbol | uppercase }}</h2>
      <p class="card-text">Price: {{ crypto.pricePerUnit | formatCurrency }}</p>
      <p class="card-text">Gain/Loss:
        <app-percent-difference [percentDifference]="crypto.investmentGainLossValue" />
      </p>
      <a (click)="cryptoDashboard(crypto.id)" class="btn btn-primary">See details</a>
    </div>
  </div>
  `,
})
export class CryptoCardComponent {
  @Input() crypto!: ViewCryptoInformation;

  private router = inject(Router);

  cryptoDashboard(cryptoID: number) {
    this.router.navigate(['/crypto-dashboard', cryptoID]);
  }
}
