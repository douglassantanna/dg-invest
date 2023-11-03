import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { CryptoInformation } from '../services/crypto.service';

@Component({
  selector: 'app-my-crypto',
  standalone: true,
  imports: [
    CommonModule],
  template: `
  <div class="card">
    <div class="card-header">
      {{ cryptoInfo.symbol }} Information
    </div>
    <ul class="list-group list-group-flush">
      <li class="list-group-item d-flex justify-content-between">
        <span>Price per Unit:</span>
        <span>{{ cryptoInfo.pricePerUnit | currency:'USD':'symbol':'1.2-2' }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between">
        <span>My Average Price:</span>
        <span>{{ cryptoInfo.myAveragePrice | currency:'USD':'symbol':'1.2-2' }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between">
        <span>% Difference:</span>
        <span>{{ cryptoInfo.percentDifference }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between">
        <span>Amount:</span>
        <span>{{ cryptoInfo.amount }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between">
        <span>Total in U$:</span>
        <span>{{ cryptoInfo.totalInUSD | currency:'USD':'symbol':'1.2-2' }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between">
        <span>Invested Amount:</span>
        <span>{{ cryptoInfo.investedAmount | currency:'USD':'symbol':'1.2-2' }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between">
        <span>Current Worth:</span>
        <span>{{ cryptoInfo.currentWorth | currency:'USD':'symbol':'1.2-2' }}</span>
      </li>
      <li class="list-group-item d-flex justify-content-between bg-primary text-white">
        <span>Investment Gain/Loss:</span>
        <span>{{ cryptoInfo.investmentGainLoss | currency:'USD':'symbol':'1.2-2' }}</span>
      </li>
    </ul>
  </div>
  `,
  styles: [``]
})
export class MyCryptoComponent {
  @Input() cryptoInfo: CryptoInformation = {} as CryptoInformation;
  myCrypto = 'Bitcoin';
  myCurrency = 'U$';
}
