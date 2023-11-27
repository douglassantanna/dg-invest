import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CreateCryptoComponent } from './create-crypto.component';
import { CryptoCardComponent } from '../components/crypto-card.component';
import { CryptoService } from '../../../core/services/crypto.service';
import { Observable, Subject, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs';
import { ViewMinimalCryptoAssetDto } from 'src/app/core/models/view-minimal-crypto-asset-dto';
@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CryptoCardComponent,
    CreateCryptoComponent,
    ReactiveFormsModule],
  template: `
    <main class="main-container">
      <header class="d-flex justify-content-between align-items-center">
        <h1>Portfolio</h1>
        <div>
          <app-create-crypto (cryptoCreated)="loadCryptoAssets()"></app-create-crypto>
        </div>
      </header>
      <div>
        <div class="row g-3">
          <div class="col mb-3 position-relative">
            <input class="form-control" placeholder="Search by name.." aria-label="Search" type="text" [formControl]="searchControl" >
            <span
              *ngIf="cryptos.length == 0">
              No assets found. Add some. 🧳</span>
          </div>
        </div>
      </div>
      <app-crypto-card [cryptos]="cryptos" />
    </main>
  `,
  styles: [`

  `]
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private unsubscribe$: Subject<void> = new Subject<void>();

  cryptos: ViewMinimalCryptoAssetDto[] = [];
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;

  ngOnDestroy() {
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }

  ngOnInit(): void {
    this.loadCryptoAssets();
    this.search();
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
        this.cryptos = cryptos.items;
      });
  }

  private search() {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((value) => {
        return this.cryptoService.getCryptoAssets(1, 10, value);
      }),
      takeUntil(this.unsubscribe$)
    ).subscribe((searchResults) => {
      this.cryptos = searchResults.items;
    });
  }
}
