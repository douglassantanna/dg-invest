import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import { AddTransactionComponent } from './add-transaction.component';
import { DataCardComponent } from '../components/my-crypto.component';
import { TransactionTableComponent } from '../components/purchase-history.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { ActivatedRoute } from '@angular/router';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { CryptoAssetData } from 'src/app/core/models/crypto-asset-data';
import { CryptoInformation } from 'src/app/core/models/crypto-information';

@Component({
  selector: 'app-crypto-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    AddTransactionComponent,
    DataCardComponent,
    TransactionTableComponent,
  ],
  template: `
  <div class="container">
    <h1>
      <strong>{{ cryptoInfo.symbol | uppercase }}</strong> Information
    </h1>
    <div class="row" >
      <div class="col" *ngFor="let card of cryptoAssetData">
        <app-data-card [title]="card.title" [value]="card.value" [percentDifference]="card.percent"/>
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
          <app-transaction-table [transactionsHistory]="transactionsHistory" />
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
