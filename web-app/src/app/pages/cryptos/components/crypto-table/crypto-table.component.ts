import { Component, Input, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { Router } from '@angular/router';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

@Component({
  selector: 'app-crypto-table',
  standalone: true,
  imports: [
    CommonModule,
    PercentDifferenceComponent,
    FormatCurrencyPipe],
  templateUrl: './crypto-table.component.html',
  styleUrls: ['./crypto-table.component.scss']
})
export class CryptoTableComponent {
  @Input() cryptos: ViewCryptoInformation[] = [];
  @Input() hideZeroBalance: boolean = false;
  avaragePrice = 5000;
  balance = 5;
  router = inject(Router);
  currentWorth = 7448;
  gainLoss = 14;
  cryptoDashboard(cryptoID: number) {
    this.router.navigate(['/crypto-dashboard', cryptoID]);
  }

  formatCurrency(amount: number): string {
    let options: Intl.NumberFormatOptions;
    if (amount < 1) {
      options = { style: 'currency', currency: 'USD', minimumFractionDigits: 6, maximumFractionDigits: 6 };
    } else {
      options = { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 };
    }
    return new Intl.NumberFormat('en-US', options).format(amount);
  }
}
