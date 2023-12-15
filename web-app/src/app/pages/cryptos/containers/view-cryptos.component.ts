import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CreateCryptoComponent } from './create-crypto.component';
import { CryptoCardComponent } from '../components/crypto-card.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { BehaviorSubject, Observable, Subject, takeUntil } from 'rxjs';
import { ViewMinimalCryptoAssetDto } from 'src/app/core/models/view-minimal-crypto-asset-dto';
import { CryptoFilterComponent } from '../components/crypto-filter.component';
import { ScreenSizeService } from 'src/app/core/services/screen-size.service';
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

      <div
        class="d-flex
               flex-column
               flex-md-row
               justify-content-between
               align-items-md-center
               pb-2">
        <div>
          <h1 *ngIf="screenSizeService.getActualScreenSize >= screenSizeService.screenSize">Portfolio</h1>
        </div>

        <ng-container *ngIf="screenSizeService.getActualScreenSize < screenSizeService.screenSize">
          <div
            class="d-flex
                  flex-md-row
                  justify-content-between
                  align-items-md-center
                  pt-2">
            <div>
              <h1>Portfolio</h1>
            </div>

            <div class="order-md-2">
              <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
            </div>
          </div>
        </ng-container>

        <div class="d-flex flex-column flex-md-row gap-2">
          <div class="order-md-1">
            <app-crypto-filter (searchControlEvent)="search($event)"></app-crypto-filter>
          </div>

          <ng-container *ngIf="screenSizeService.getActualScreenSize >= screenSizeService.screenSize">
            <div class="order-md-2">
              <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
            </div>
          </ng-container>
        </div>
      </div>

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
  `]
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private unsubscribe$: Subject<void> = new Subject<void>();
  screenSizeService = inject(ScreenSizeService);

  cryptos$: BehaviorSubject<ViewMinimalCryptoAssetDto[]> = new BehaviorSubject<ViewMinimalCryptoAssetDto[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;

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
    pageSize: number = 10,
    cryptoCurrency: string = "",
    sortOrder: string = "ASC"
  ) {
    this.cryptoService.getCryptoAssets(page, pageSize, cryptoCurrency, sortOrder)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe(cryptos => {
        this.cryptos$.next(cryptos.items);
      });
  }

  search(input: any) {
    this.cryptoService.getCryptoAssets(1, 10, input)
      .pipe(
        takeUntil(this.unsubscribe$)
      ).subscribe((searchResults) => {
        this.cryptos$.next(searchResults.items);
      });
  }
}
