import { Crypto } from './../../../core/models/crypto';
import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CreateCryptoComponent } from './create-crypto.component';
import { CryptoCardComponent } from '../components/crypto-card.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { BehaviorSubject, Observable, Subject, takeUntil } from 'rxjs';
import { CryptoFilterComponent } from '../components/crypto-filter.component';
import { CryptoTableComponent } from '../components/crypto-table/crypto-table.component';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { PercentDifferenceComponent } from '../components/percent-difference.component';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CryptoCardComponent,
    CreateCryptoComponent,
    ReactiveFormsModule,
    CryptoFilterComponent,
    CryptoTableComponent,
    PercentDifferenceComponent],
  template: `
    <main class="container">
      <header>
        <div class="coll-1">
          <h1>Cryptos</h1>
        </div>

        <ng-container *ngIf="$emptyCryptoArray | async">
          <div class="coll-2">
            <app-crypto-filter
            (viewDataTableEvent)="displayDataTableView($event)"
            (searchControlEvent)="search($event, hideZeroBalance)"
            (hideZeroBalanceControlEvent)="search('', hideZeroBalance = $event)"
            [setBalanceStatus]="setBalanceStatus">
          </app-crypto-filter>
        </div>

        <div class="coll-3">
          <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
        </div>
      </ng-container>
    </header>

    <div class="row">
      <div class="container mt-3">
        <div class="row">
          <div class="col-md-6">
          <div class="alert alert-light">
            <strong>Total Invested:</strong> {{ totalInvested | currency:'USD':'symbol':'1.2-2'}}
          </div>
        </div>
        <div class="col-md-6">
          <div class="alert alert-light">
            <strong>Total Market Value:</strong> {{ totalMarketValue | currency:'USD':'symbol':'1.2-2'}}
            <app-percent-difference [percentDifference]="investmentChangePercent"></app-percent-difference>
          </div>
        </div>
      </div>
      </div>

        <div *ngIf="cryptos$ | async as cryptos; else loading">
          <ng-container *ngIf="cryptos?.length; else emptyCriptoList">
            <ng-template #cardView>
              <div class="row">
                <div class="col-md-4" *ngFor="let crypto of cryptos">
                  <app-crypto-card [crypto]="crypto" />
                </div>
              </div>
            </ng-template>
            <div class="row" *ngIf="displayDataTable;else cardView">
              <div class="col-md-12">
                <app-crypto-table [cryptos]="cryptos" [hideZeroBalance]="hideZeroBalance" />
              </div>
            </div>
          </ng-container>
        </div>

        <ng-template #loading>
          <div class="text-center">Loading...</div>
        </ng-template>
      </div>

      <ng-template #emptyCriptoList>
        <div class="text-center">
          <h2>No assets found 😥</h2>
          <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
        </div>
      </ng-template>

    </main>
  `,
  styles: [`
    header {
      display: flex;
      flex-wrap: wrap;
    }

    .coll-1,
    .coll-2,
    .coll-3 {
      padding: 5px 0px 5px 5px;
    }
    .coll-1{ width: 50%; }
    .coll-2{ width: 40%; }
    .coll-3{ width: 10%; display: flex; justify-content: flex-end;}

    @media screen and (max-width: 768px) {
      .coll-1{ width: 70%; justify-content: space-between; order: 1;}
      .coll-2{ flex: 1; width: 100%; order: 3; }
      .coll-3{ width: 30%; order: 2; display: flex; justify-content: flex-end; }
      }
`]
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private localStorageService = inject(LocalStorageService);
  private unsubscribe$: Subject<void> = new Subject<void>();

  cryptos$: BehaviorSubject<ViewCryptoInformation[]> = new BehaviorSubject<ViewCryptoInformation[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;
  hideZeroBalance: boolean = false;
  displayDataTable: boolean = true;
  $emptyCryptoArray: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  totalInvested = 0;
  totalMarketValue = 0;
  investmentChangePercent = 0;
  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngOnInit(): void {
    this.loadCryptoAssets();
    this.displayDataTable = this.localStorageService.getDataViewType();
  }

  loadCryptoAssets(
    page: number = 1,
    pageSize: number = 50,
    cryptoCurrency: string = "",
    sortOrder: string = "ASC",
    hideZeroBalance = this.localStorageService.getHideZeroBalance()
  ) {
    this.cryptoService.getCryptoAssets(page, pageSize, cryptoCurrency, sortOrder, hideZeroBalance)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe({
        next: (cryptos) => {
          this.cryptos$.next(cryptos.items);
          this.$emptyCryptoArray.next(cryptos.items.length > 0);
          this.totalInvested = this.sumTotalInvested(cryptos.items);
          this.totalMarketValue = this.sumTotalMarketValue(cryptos.items);
          this.investmentChangePercent = this.calculatePercentDifference(cryptos.items);
        }
      });
  }

  displayDataTableView(event: boolean) {
    this.displayDataTable = event;
    this.localStorageService.setDataViewType(event);
  }

  search(input: string, hideZeroBalance: boolean) {
    this.localStorageService.setHideZeroBalance(hideZeroBalance);
    this.cryptoService.getCryptoAssets(1, 50, input, "ASC", hideZeroBalance)
      .pipe(
        takeUntil(this.unsubscribe$)
      ).subscribe({
        next: (cryptos) => {
          this.cryptos$.next(cryptos.items);
        },
        error: (err) => {
          this.setBalanceStatus(false);
        },
      });
  }
  setBalanceStatus(value: boolean): void {
    this.hideZeroBalance = value;
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
