import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, resolveForwardRef } from '@angular/core';

import { AddTransactionComponent } from './add-transaction.component';
import { MyCryptoComponent } from './my-crypto.component';
import { PurchaseHistoryComponent } from './purchase-history.component';
import { CryptoAssetData, CryptoInformation, CryptoService, ETransactionType } from '../services/crypto.service';
import { ActivatedRoute } from '@angular/router';

export interface CryptoTransactionHistory {
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
    .grid-container {
      display: grid;
      grid-template-columns: 1fr 1fr;
      grid-template-rows: 0.4fr 1.6fr;
      gap: 10px 10px;
      grid-template-areas:
        "div1 div3"
        "div2 div3";
      padding:10px;
    }
    .div1 {
      grid-area:div1;
    }
    .div2 {
      grid-area:div2;
    }
    .div3 {
      grid-area:div3;
      max-height:600px;
      overflow:auto;
    }
    @media (max-width: 640px) {
        .grid-container {
          grid-template-columns: 1fr;
          grid-template-rows: 0.4fr;
          grid-template-areas:
          "div1"
          "div2"
          "div3";
          justify-content: center;
          align-items: center;
          gap: 5px;
        }
      }
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
      this.cryptoService.getCryptoAssetById(this.cryptoAssetId).subscribe(response => {
        console.log(response);
        this.cryptoInfo = response.data.cryptoInformation;
        this.transactionsHistory = response.data.transactions;
        this.cryptoAssetData = response.data.cryptoAssetData;
      })
    })
  }
}
