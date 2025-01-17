import { UpperCasePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';

import { TransactionTableComponent } from '../../components/transaction-table/transaction-table.component';
import { CryptoService } from '../../../../core/services/crypto.service';
import { ActivatedRoute } from '@angular/router';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { CryptoAssetData } from 'src/app/core/models/crypto-asset-data';
import { CryptoInformation } from 'src/app/core/models/crypto-information';
import { map, Subject, switchMap, takeUntil, tap } from 'rxjs';
import { AddTransactionComponent } from '../../components/add-transaction/add-transaction.component';
import { StatsCardComponent } from '../../components/stats-card/stats-card.component';

@Component({
  selector: 'app-crypto-details',
  standalone: true,
  imports: [
    UpperCasePipe,
    AddTransactionComponent,
    TransactionTableComponent,
    StatsCardComponent
  ],
  templateUrl: './crypto-details.component.html',
})
export class CryptoDetailsComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private route = inject(ActivatedRoute);
  cryptoAssetId = signal(0);
  cryptoInfo = signal({} as CryptoInformation);
  private unsubscribe$ = new Subject<void>();
  transactions = signal<CryptoTransactionHistory[]>([]);
  cryptoAssetData = signal<CryptoAssetData[]>([]);
  isLoading = signal(true);
  isCryptoAssetsLoading = signal(false);
  isTransactionDataLoading = signal(false);

  ngOnInit(): void {
    this.isLoading.set(true);
    this.isCryptoAssetsLoading.set(true);
    this.isTransactionDataLoading.set(true);

    this.route.params
      .pipe(
        map(params => params['cryptoId']),
        tap(id => (this.cryptoAssetId.set(id))),
        switchMap(id => this.cryptoService.getCryptoAssetById(id)),
      ).subscribe(response => {
        this.cryptoInfo.set(response.data.cryptoInformation);
        this.isLoading.set(false);
        this.isTransactionDataLoading.set(false);
        this.isCryptoAssetsLoading.set(false);
      });

    this.cryptoService.cryptoAssetData$
      .subscribe((responsee: CryptoAssetData[]) => {
        this.cryptoAssetData.set(responsee);
      });

    this.cryptoService.transactions$
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
