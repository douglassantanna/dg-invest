import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CreateCryptoComponent } from './create-crypto.component';
import { CryptoCardComponent } from '../components/crypto-card.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { BehaviorSubject, Observable, Subject, takeUntil } from 'rxjs';
import { ViewMinimalCryptoAssetDto } from 'src/app/core/models/view-minimal-crypto-asset-dto';
import { CryptoFilterComponent } from '../components/crypto-filter.component';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CryptoCardComponent,
    CreateCryptoComponent,
    ReactiveFormsModule,
    CryptoFilterComponent],
  template: `
    <main class="container">
      <header>
        <div class="coll-1">
          <h1>Portfolio</h1>
        </div>

        <div class="coll-2">
          <app-crypto-filter
            (searchControlEvent)="search($event, hideZeroBalance)"
            (hideZeroBalanceControlEvent)="search('', hideZeroBalance = $event)"
            [setBalanceStatus]="setBalanceStatus">
          </app-crypto-filter>
        </div>

        <div class="coll-3">
          <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
        </div>
      </header>

      <div class="row">
        <div *ngIf="cryptos$ | async as cryptos; else loading">
          <div class="row">
            <div class="col-md-4" *ngFor="let crypto of cryptos">
              <app-crypto-card [crypto]="crypto" />
            </div>
          </div>
        </div>

        <ng-template #loading>
          <div class="text-center">Loading...</div>
        </ng-template>
      </div>

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
  private unsubscribe$: Subject<void> = new Subject<void>();

  cryptos$: BehaviorSubject<ViewMinimalCryptoAssetDto[]> = new BehaviorSubject<ViewMinimalCryptoAssetDto[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;
  hideZeroBalance: boolean = false;;
  constructor() {
  }

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngOnInit(): void {
    this.loadCryptoAssets();
  }

  loadCryptoAssets(
    page: number = 1,
    pageSize: number = 50,
    cryptoCurrency: string = "",
    sortOrder: string = "ASC",
    hideZeroBalance = false
  ) {
    this.cryptoService.getCryptoAssets(page, pageSize, cryptoCurrency, sortOrder, hideZeroBalance)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe({
        next: (cryptos) => {
          this.cryptos$.next(cryptos.items);
        }
      });
  }

  search(input: string, hideZeroBalance: boolean) {
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
}
