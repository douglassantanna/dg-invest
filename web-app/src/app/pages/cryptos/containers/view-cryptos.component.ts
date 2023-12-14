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

    <div class="d-flex justify-content-between align-items-center p-2">
      <div>
        <h1>Portfolio</h1>
      </div>

      <div class="d-flex gap-2">
        <div>
          <app-crypto-filter (searchControlEvent)="search($event)"></app-crypto-filter>
        </div>

        <div>
          <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
        </div>
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

  cryptos$: BehaviorSubject<ViewMinimalCryptoAssetDto[]> = new BehaviorSubject<ViewMinimalCryptoAssetDto[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;

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
