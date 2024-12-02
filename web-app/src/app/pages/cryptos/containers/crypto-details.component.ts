import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';

import { DataCardComponent } from '../components/data-card.component';
import { TransactionTableComponent } from '../components/transaction-table.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { ActivatedRoute } from '@angular/router';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { CryptoAssetData } from 'src/app/core/models/crypto-asset-data';
import { CryptoInformation } from 'src/app/core/models/crypto-information';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';
import { AddTransactionComponent } from '../components/add-transaction/add-transaction.component';

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
      <strong>{{ cryptoInfo.symbol | uppercase }}</strong>
    </h1>
    <div class="row" >
      <div class="col" *ngFor="let card of cryptoAssetData$ | async; trackBy: cardValue">
        <app-data-card [title]="card.title" [value]="card.value" [percentDifference]="card.percent ? card.percent : 0"/>
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
  .container{
    padding: 16px;
  }
  .crypto-details-container {
  padding: 16px;
}

.crypto-details-container h1 {
  font-size: 32px;
  margin-bottom: 16px;
}

.card-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  margin-bottom: 32px;
}

.card-col {
  flex: 1 1 calc(50% - 16px);
}

.transaction-row {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
}

.transaction-col {
  flex: 1 1 calc(50% - 16px);
}

.transaction-item {
  margin: 16px 0;
}

/* Mobile styles */
@media (max-width: 600px) {
  .card-col,
  .transaction-col {
    flex: 1 1 100%;
  }
}
  `]
})
export class CryptoDetailsComponent implements OnInit, OnDestroy {

  private cryptoService = inject(CryptoService);
  private route = inject(ActivatedRoute);
  cryptoAssetId = 0;
  cryptoInfo: CryptoInformation = {} as CryptoInformation;

  private unsubscribe$ = new Subject<void>();

  transactions$: BehaviorSubject<CryptoTransactionHistory[]> = new BehaviorSubject<CryptoTransactionHistory[]>([]);

  cryptoAssetData$: BehaviorSubject<CryptoAssetData[]> = new BehaviorSubject<CryptoAssetData[]>([]);

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.cryptoAssetId = params['cryptoId'];
    });

    this.cryptoService.getCryptoAssetById(this.cryptoAssetId)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(response => {
        this.cryptoInfo = response.data.cryptoInformation;
      });

    this.cryptoService.cryptoAssetData$
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe((responsee: CryptoAssetData[]) => {
        this.cryptoAssetData$.next(responsee);
      });

    this.cryptoService.transactions$
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(transactions => {
        this.transactions$.next(transactions);
      });
  }

  ngOnDestroy(): void {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  cardValue(index: number, dataValue: CryptoAssetData) {
    return dataValue.value;
  }
}
