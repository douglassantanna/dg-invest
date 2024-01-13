import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { Router } from '@angular/router';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';

@Component({
  selector: 'app-crypto-table',
  standalone: true,
  imports: [
    CommonModule,
    PercentDifferenceComponent],
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
}
