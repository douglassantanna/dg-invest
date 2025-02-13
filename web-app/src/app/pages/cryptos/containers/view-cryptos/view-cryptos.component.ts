import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CryptoService } from '../../../../core/services/crypto.service';
import { Subject, takeUntil } from 'rxjs';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CryptoTableComponent } from '../../components/crypto-table/crypto-table.component';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { PercentDifferenceComponent } from '../../components/percent-difference.component';
import { PieChartComponent } from '../../components/pie-chart/pie-chart.component';
import { RouterModule } from '@angular/router';
import { AddCryptoComponent } from '../../components/add-crypto/add-crypto.component';
import { CurrencyPipe } from '@angular/common';
import { ModalComponent } from 'src/app/layout/modal/modal.component';
import { LineChartComponent } from '../../components/line-chart/line-chart.component';
import { PortfolioCardComponent } from '../../components/portfolio-card/portfolio-card.component';
import { LayoutService } from 'src/app/core/services/layout.service';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CryptoFilterComponent,
    CryptoTableComponent,
    PieChartComponent,
    RouterModule,
    AddCryptoComponent,
    ModalComponent,
    LineChartComponent,
    PortfolioCardComponent],
  templateUrl: 'view-cryptos.component.html'
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private localStorageService = inject(LocalStorageService);
  private unsubscribe$: Subject<void> = new Subject<void>();
  cryptoAssetList = signal<ViewCryptoInformation[]>([]);
  hideZeroBalance: boolean = false;
  isCryptoAssetListEmpty = signal(false);
  totalInvested = signal(0);
  totalMarketValue = signal(0);
  investmentChangePercent = signal(0);
  accountBalance = signal(0);
  isModalOpen = signal(false);
  loading = signal(false);

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngOnInit(): void {
    const params = this.getCryptoAssetParams();
    this.loadCryptoAssets(params);
  }

  toggleCreateCryptoModal(cryptoCreated: any = null) {
    if (cryptoCreated) {
      const params = this.getCryptoAssetParams();
      this.loadCryptoAssets(params);
    }
    this.isModalOpen.set(!this.isModalOpen());
  }

  private loadCryptoAssets(params: any = {}) {
    const sortByLocalStorage = this.localStorageService.getAssetListSortBy() ?? 'symbol';
    const sortOrderLocalStorage = this.localStorageService.getAssetListSortOrder() ?? 'asc';
    const page = params.page ?? 1;
    const pageSize = params.pageSize ?? 50;
    const sortBy = params.sortBy ?? sortByLocalStorage;
    const sortOrder = params.sortOrder ?? sortOrderLocalStorage;
    const assetName = params.assetName ?? '';
    const hideZeroBalance = params.hideZeroBalance ? true : false;
    this.loading.set(true);
    this.cryptoService.getCryptoAssets(page, pageSize, assetName, sortBy, sortOrder, hideZeroBalance)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe({
        next: (response) => {
          const portfolioArray = response.items[0].cryptoAssetDto;
          this.accountBalance.set(response.items[0].accountBalance);
          this.isCryptoAssetListEmpty.set(portfolioArray.length > 0);
          this.totalInvested.set(this.sumTotalInvested(portfolioArray));
          this.totalMarketValue.set(this.sumTotalMarketValue(portfolioArray));
          this.investmentChangePercent.set(this.calculatePercentDifference(portfolioArray));
          this.cryptoAssetList.set(portfolioArray);
          this.loading.set(false);
        },
        error: (err) => {
          console.log('HTTP call failed:', err);
          this.updateLocalStorageSortOrder();
          this.loading.set(false);
        }
      });
  }

  search(input: string, hideZeroBalance: boolean) {
    this.loading.set(true);
    this.localStorageService.setHideZeroBalance(hideZeroBalance);
    this.cryptoService.getCryptoAssets(1, 50, input, "symbol", "asc", hideZeroBalance)
      .pipe(
        takeUntil(this.unsubscribe$)
      ).subscribe({
        next: (response) => {
          this.loading.set(false);
          const portfolioArray = response.items[0].cryptoAssetDto;
          this.accountBalance.set(response.items[0].accountBalance);
          this.isCryptoAssetListEmpty.set(portfolioArray.length > 0);
          this.totalInvested.set(this.sumTotalInvested(portfolioArray));
          this.totalMarketValue.set(this.sumTotalMarketValue(portfolioArray));
          this.investmentChangePercent.set(this.calculatePercentDifference(portfolioArray));
          this.cryptoAssetList.set(portfolioArray);
        },
        error: (err) => {
          this.loading.set(false);
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
