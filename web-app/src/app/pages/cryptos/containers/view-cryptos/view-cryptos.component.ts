import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CreateCryptoComponent } from '../create-crypto.component';
import { CryptoService } from '../../../../core/services/crypto.service';
import { Observable, Subject, takeUntil } from 'rxjs';
import { CryptoFilterComponent } from '../../components/crypto-filter.component';
import { CryptoTableComponent } from '../../components/crypto-table/crypto-table.component';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { PercentDifferenceComponent } from '../../components/percent-difference.component';
import { PieChartComponent } from '../../components/pie-chart/pie-chart.component';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CreateCryptoComponent,
    ReactiveFormsModule,
    CryptoFilterComponent,
    CryptoTableComponent,
    PercentDifferenceComponent,
    PieChartComponent],
  templateUrl: 'view-cryptos.component.html'
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private localStorageService = inject(LocalStorageService);
  private unsubscribe$: Subject<void> = new Subject<void>();
  cryptoAssetList = signal<ViewCryptoInformation[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;
  hideZeroBalance: boolean = false;
  isCryptoAssetListEmpty = signal(false);
  totalInvested = 0;
  totalMarketValue = 0;
  investmentChangePercent = 0;
  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngOnInit(): void {
    this.loadCryptoAssets();
  }

  loadCryptoAssets(params: any = {}) {
    const page = params.page ?? 1;
    const pageSize = params.pageSize ?? 50;
    const sortBy = params.sortBy ?? 'symbol';
    const sortOrder = params.sortOrder ?? 'asc';
    const assetName = params.assetName ?? '';
    const hideZeroBalance = params.hideZeroBalance ?? false;

    this.cryptoService.getCryptoAssets(page, pageSize, assetName, sortBy, sortOrder, hideZeroBalance)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe({
        next: (cryptos) => {
          this.isCryptoAssetListEmpty.set(cryptos.items.length > 0);
          this.totalInvested = this.sumTotalInvested(cryptos.items);
          this.totalMarketValue = this.sumTotalMarketValue(cryptos.items);
          this.investmentChangePercent = this.calculatePercentDifference(cryptos.items);
          this.cryptoAssetList.set(cryptos.items);
        }
      });
  }

  search(input: string, hideZeroBalance: boolean) {
    this.localStorageService.setHideZeroBalance(hideZeroBalance);
    this.cryptoService.getCryptoAssets(1, 50, input, "symbol", "asc", hideZeroBalance)
      .pipe(
        takeUntil(this.unsubscribe$)
      ).subscribe({
        next: (cryptos) => {
          this.cryptoAssetList.set(cryptos.items);
          this.totalInvested = this.sumTotalInvested(cryptos.items);
          this.totalMarketValue = this.sumTotalMarketValue(cryptos.items);
          this.investmentChangePercent = this.calculatePercentDifference(cryptos.items);
        },
        error: (err) => {
          this.setBalanceStatus(false);
        },
      });
  }

  setBalanceStatus(value: boolean): void {
    this.hideZeroBalance = value;
  }

  outputHeaderEvent(event: string) {
    const params = {
      page: 1,
      pageSize: 50,
      assetName: '',
      sortBy: event,
      sortOrder: 'asc',
      hideZeroBalance: this.localStorageService.getHideZeroBalance()
    };

    this.loadCryptoAssets(params);
  }

  private sumTotalInvested(cryptos: ViewCryptoInformation[]): number {
    return cryptos.reduce((acc, cur) => acc + cur.investedAmount, 0);
  }

  private sumTotalMarketValue(cryptos: ViewCryptoInformation[]): number {
    return cryptos.reduce((acc, cur) => acc + cur.currentWorth, 0);
  }

  private calculatePercentDifference(cryptos: ViewCryptoInformation[]): number {
    const totalInvested = this.sumTotalInvested(cryptos);
    const totalMarketValue = this.sumTotalMarketValue(cryptos);

    if (totalInvested === 0) {
      return 0;
    }

    const percentDifference = ((totalMarketValue - totalInvested) / totalInvested) * 100;
    return percentDifference;
  }
}
