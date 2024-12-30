import { AsyncPipe, UpperCasePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';

import { TransactionTableComponent } from '../../components/transaction-table/transaction-table.component';
import { CryptoService } from '../../../../core/services/crypto.service';
import { ActivatedRoute } from '@angular/router';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { CryptoAssetData } from 'src/app/core/models/crypto-asset-data';
import { CryptoInformation } from 'src/app/core/models/crypto-information';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';
import { AddTransactionComponent } from '../../components/add-transaction/add-transaction.component';
import { StatsCardComponent } from '../../components/stats-card/stats-card.component';

@Component({
  selector: 'app-crypto-details',
  standalone: true,
  imports: [
    UpperCasePipe,
    AsyncPipe,
    AddTransactionComponent,
    TransactionTableComponent,
    StatsCardComponent
  ],
  templateUrl: './crypto-details.component.html',
})
export class CryptoDetailsComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private route = inject(ActivatedRoute);
  cryptoAssetId = 0;
  cryptoInfo: CryptoInformation = {} as CryptoInformation;
  private unsubscribe$ = new Subject<void>();
  transactions = signal<CryptoTransactionHistory[]>([]);
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
        this.transactions.set(transactions);
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
