import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormControl } from '@angular/forms';

import { CryptoService } from '../../../../core/services/crypto.service';
import { Observable, Subject, takeUntil } from 'rxjs';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CryptoTableComponent } from '../../components/crypto-table/crypto-table.component';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { PercentDifferenceComponent } from '../../components/percent-difference.component';
import { PieChartComponent } from '../../components/pie-chart/pie-chart.component';
import { Router, RouterModule } from '@angular/router';
import { AddCryptoComponent } from '../../components/add-crypto/add-crypto.component';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CryptoFilterComponent,
    CryptoTableComponent,
    PercentDifferenceComponent,
    PieChartComponent,
    RouterModule,
    AddCryptoComponent,
    CurrencyPipe],
  templateUrl: 'view-cryptos.component.html'
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private localStorageService = inject(LocalStorageService);
  private readonly router = inject(Router);
  private unsubscribe$: Subject<void> = new Subject<void>();
  cryptoAssetList = signal<ViewCryptoInformation[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;
  hideZeroBalance: boolean = false;
  isCryptoAssetListEmpty = signal(false);
  totalInvested = 0;
  totalMarketValue = 0;
  investmentChangePercent = 0;
  accountBalance = signal(0);
  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngOnInit(): void {
    const params = this.getCryptoAssetParams();
    this.loadCryptoAssets(params);
  }

  redirectToAccount() {
    this.router.navigateByUrl('/account');
  }
  loadCryptoAssets(params: any = {}) {
    const sortByLocalStorage = this.localStorageService.getAssetListSortBy() ?? 'symbol';
    const sortOrderLocalStorage = this.localStorageService.getAssetListSortOrder() ?? 'asc';
    const page = params.page ?? 1;
    const pageSize = params.pageSize ?? 50;
    const sortBy = params.sortBy ?? sortByLocalStorage;
    const sortOrder = params.sortOrder ?? sortOrderLocalStorage;
    const assetName = params.assetName ?? '';
    const hideZeroBalance = params.hideZeroBalance ? true : false;

    this.cryptoService.getCryptoAssets(page, pageSize, assetName, sortBy, sortOrder, hideZeroBalance)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe({
        next: (cryptos) => {
          const cryptoArray = cryptos.items.map((item) => item.cryptoAssetDto);
          const accountBalance = cryptos.items.map((item) => item.accountBalance)[0];
          this.accountBalance.set(accountBalance);
          this.isCryptoAssetListEmpty.set(cryptos.items.length > 0);
          this.totalInvested = this.sumTotalInvested(cryptoArray);
          this.totalMarketValue = this.sumTotalMarketValue(cryptoArray);
          this.investmentChangePercent = this.calculatePercentDifference(cryptoArray);
          this.cryptoAssetList.set(cryptoArray);
        },
        error: (err) => {
          console.log('HTTP call failed:', err);
          this.updateLocalStorageSortOrder();
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
          const cryptoArray = cryptos.items.map((item) => item.cryptoAssetDto);
          const accountBalance = cryptos.items.map((item) => item.accountBalance)[0];
          this.accountBalance.set(accountBalance);
          this.cryptoAssetList.set(cryptoArray);
          this.totalInvested = this.sumTotalInvested(cryptoArray);
          this.totalMarketValue = this.sumTotalMarketValue(cryptoArray);
          this.investmentChangePercent = this.calculatePercentDifference(cryptoArray);
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
    const newSortOrder = this.updateLocalStorageSortOrder();

    if (event === 'symbol' || event === 'invested_amount') {
      this.localStorageService.setAssetListSortBy(event);
    }

    const params = this.getCryptoAssetParams(newSortOrder);
    this.loadCryptoAssets(params);
  }

  sortOrderOutput(): string {
    return this.localStorageService.getAssetListSortOrder() === 'asc' ? 'asc' : 'desc';
  }

  sortByOutput(): string {
    return this.localStorageService.getAssetListSortBy();
  }

  private updateLocalStorageSortOrder(): string {
    const currentSortOrder = this.localStorageService.getAssetListSortOrder();
    const newSortOrder = currentSortOrder === 'asc' ? 'desc' : 'asc';
    this.localStorageService.setAssetListSortOrder(newSortOrder);
    return newSortOrder;
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

  private getCryptoAssetParams(sortOrderOverride?: string): { page: number, pageSize: number, assetName: string, sortBy: string, sortOrder: string, hideZeroBalance: boolean } {
    return {
      page: 1,
      pageSize: 50,
      assetName: '',
      sortBy: this.localStorageService.getAssetListSortBy() || 'symbol',
      sortOrder: sortOrderOverride || this.localStorageService.getAssetListSortOrder() || 'asc',
      hideZeroBalance: this.localStorageService.getHideZeroBalance() ?? false
    };
  }
}
