import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';

import { AddTransactionComponent } from './add-transaction.component';
import { DataCardComponent } from '../components/data-card.component';
import { TransactionTableComponent } from '../components/transaction-table.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { ActivatedRoute } from '@angular/router';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { CryptoAssetData } from 'src/app/core/models/crypto-asset-data';
import { CryptoInformation } from 'src/app/core/models/crypto-information';
import { BehaviorSubject } from 'rxjs';

@Component({
  selector: 'app-crypto-details',
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
      <div class="col" *ngFor="let card of cryptoAssetData$ | async; trackBy: cardValue">
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
          <app-transaction-table [transactions]="transactions$" />
        </div>
      </div>
    </div>
  </div>
  `,
  styles: [`
  `]
})
export class CryptoDetailsComponent implements OnInit {
  private cryptoService = inject(CryptoService);
  private route = inject(ActivatedRoute);
  cryptoAssetId = 0;
  cryptoInfo: CryptoInformation = {} as CryptoInformation;

  transactions$: BehaviorSubject<CryptoTransactionHistory[]> = new BehaviorSubject<CryptoTransactionHistory[]>([]);

  cryptoAssetData$: BehaviorSubject<CryptoAssetData[]> = new BehaviorSubject<CryptoAssetData[]>([]);

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.cryptoAssetId = params['cryptoId'];
    })

    this.cryptoService.getCryptoAssetById(this.cryptoAssetId).subscribe(response => {
      this.cryptoInfo = response.data.cryptoInformation;
    })

    this.cryptoService.cryptoAssetData$.subscribe((responsee: CryptoAssetData[]) => {
      this.cryptoAssetData$.next(responsee);
    });

    this.cryptoService.transactions$.subscribe(transactions => {
      this.transactions$.next(transactions);
    })

  }

  cardValue(index: number, dataValue: CryptoAssetData) {
    return dataValue.value;
  }
}
