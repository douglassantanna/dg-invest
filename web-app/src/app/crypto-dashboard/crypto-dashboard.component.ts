import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import { AddTransactionComponent } from '../pages/cryptos/containers/add-transaction.component';
import { MyCryptoComponent } from '../pages/cryptos/components/my-crypto.component';
import { PurchaseHistoryComponent } from '../pages/cryptos/components/purchase-history.component';
import { CryptoAssetData, CryptoInformation, CryptoService, ETransactionType } from '../core/services/crypto.service';
import { ActivatedRoute } from '@angular/router';

export interface CryptoTransactionHistory {
  id: number;
  amount: number;
  price: number;
  purchaseDate: number;
  exchangeName: string;
  transactionType: ETransactionType;
}

@Component({
  selector: 'app-crypto-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    AddTransactionComponent,
    MyCryptoComponent,
    PurchaseHistoryComponent,
  ],
  template: `
  <div class="container">
    <h1>
      <strong>{{ cryptoInfo.symbol | uppercase }}</strong> Information
    </h1>
    <div class="row" >
      <div class="col" *ngFor="let card of cryptoAssetData">
        <app-my-crypto [title]="card.title" [value]="card.value" [percentDifference]="card.percent"/>
      </div>
    </div>

    <div class="row">
      <div class="col-md-6">
        <div class="m-2">
          <app-add-transaction [cryptoAssetId]="cryptoAssetId"/>
        </div>
      </div>

      <div class="col-md-6">
        <div class="m-2">
          <app-purchase-history [transactionsHistory]="transactionsHistory" />
        </div>
      </div>
    </div>
  </div>
  `,
  styles: [`
  `]
})
export class CryptoDashboardComponent implements OnInit {
  private cryptoService = inject(CryptoService);
  private route = inject(ActivatedRoute);
  cryptoAssetId = 0;
  cryptoInfo: CryptoInformation = {} as CryptoInformation;
  transactionsHistory: CryptoTransactionHistory[] = [];
  cryptoAssetData: CryptoAssetData[] = [];
  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.cryptoAssetId = params['cryptoId'];
    })
    this.cryptoService.getCryptoAssetById(this.cryptoAssetId).subscribe(response => {
      this.cryptoInfo = response.data.cryptoInformation;
      this.transactionsHistory = response.data.transactions;
      this.cryptoAssetData = response.data.cryptoAssetData;
    })
  }
}
