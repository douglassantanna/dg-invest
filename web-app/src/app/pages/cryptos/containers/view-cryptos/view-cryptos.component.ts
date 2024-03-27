import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { CryptoService } from '../../../../core/services/crypto.service';
import { BehaviorSubject, Observable, Subject, takeUntil } from 'rxjs';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CryptoTableComponent } from '../../components/crypto-table/crypto-table.component';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { LocalStorageService } from 'src/app/core/services/local-storage.service';
import { CreateAssetComponent } from '../../components/create-asset/create-asset.component';
import { ButtonComponent } from 'src/app/layout/button/button.component';
import { MatDialog } from '@angular/material/dialog';
import { DividerComponent } from 'src/app/layout/divider/divider.component';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CryptoFilterComponent,
    CryptoTableComponent,
    CreateAssetComponent,
    ButtonComponent,
    DividerComponent,
    MatButtonModule,
    MatIconModule],
  templateUrl: 'view-cryptos.component.html'
})
export class ViewCryptosComponent implements OnInit, OnDestroy {
  private cryptoService = inject(CryptoService);
  private dialog = inject(MatDialog);
  private localStorageService = inject(LocalStorageService);
  private unsubscribe$: Subject<void> = new Subject<void>();

  cryptos$: BehaviorSubject<ViewCryptoInformation[]> = new BehaviorSubject<ViewCryptoInformation[]>([]);
  searchControl: FormControl = new FormControl();
  results$!: Observable<any[]>;
  hideZeroBalance: boolean = false;
  emptyCryptoList!: boolean;

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
    hideZeroBalance = this.localStorageService.getHideZeroBalance()
  ) {
    this.cryptoService.getCryptoAssets(page, pageSize, cryptoCurrency, sortOrder, hideZeroBalance)
      .pipe(takeUntil(this.unsubscribe$))
      .subscribe({
        next: (cryptos) => {
          this.cryptos$.next(cryptos.items);
          this.emptyCryptoList = !cryptos.items.length;
        }
      });
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

  openModal() {
    const dialogRef = this.dialog.open(CreateAssetComponent, {
      width: '350px'
    });
  }
}
