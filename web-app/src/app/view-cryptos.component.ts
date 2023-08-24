import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';

import { CryptoX } from './interfaces/crypto.model';
import { CreateCryptoComponent } from './create-crypto.component';
import { AddTransactionComponent } from './add-transaction.component';
import { CryptoService, ViewCrypto } from './services/crypto.service';
import { take } from 'rxjs';

@Component({
  selector: 'app-view-cryptos',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    FormsModule,
    MatSelectModule,
    MatDialogModule],
  template: `
      <main class="main-container">
        <header>
          <h1>Portfolio</h1>
          <div class="filters">

        <button
          type="button"
          mat-raised-button
          color="primary"
          (click)="createCrypto()">
          <mat-icon>add</mat-icon> Add Crypto
        </button>
            <mat-form-field appearance="outline" style="margin-left: 10px;">
              <mat-label>Search by name..</mat-label>
              <input matInput type="text" [(ngModel)]="searchValue">
              <button
                color="primary"
                matSuffix
                mat-icon-button
                aria-label="Search"
                (click)="listCryptos()">
                <mat-icon>search</mat-icon>
              </button>
            </mat-form-field>

            <mat-form-field appearance="outline" style="margin-left: 10px;">
              <mat-label>Order</mat-label>
              <mat-select>
                <mat-option *ngFor="let option of orderOptions" [value]="option">
                  {{option}}
                </mat-option>
              </mat-select>
            </mat-form-field>
          </div>

        </header>
      <div class="crypto-container">
        <mat-card *ngFor="let crypto of cryptos" class="crypto-card">
          <mat-card-header>
            <mat-card-title>{{ crypto.cryptoCurrency }}</mat-card-title>
            <mat-card-subtitle>{{ crypto.symbol }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p>Price: $</p>
            <p>Average Price: $ {{ crypto.averagePrice }}</p>
            <p>Price Difference: {{ crypto.priceDifferencePercent | number: '1.2-2' }}%</p>
          </mat-card-content>
          <mat-card-actions align="end">
            <button mat-button color="primary" (click)="cryptoDashboard(crypto.id)">Ver mais</button>
          </mat-card-actions>
        </mat-card>
      </div>
    </main>
  `,
  styles: [`
    .main-container {
        display: flex;
        flex-direction: column;
        padding:20px;
      }
    header{
      display: flex;
      justify-content:space-between;
      align-items:center;
      gap:10px;
    }
    .filters{
      padding:5px;
    }
    .crypto-container {
      display: grid;
        grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
        grid-gap: 1rem;
        margin-bottom: 1rem;
    }
    mat-form-field{
      width:20vw;
    }
    @media (max-width: 640px) {
        .crypto-container {
          grid-template-columns: 1fr;
          justify-content: center;
          align-items: center;
        }
        header{
          flex-direction:column;
          align-items:center;
        }
        mat-form-field{
          width:100%;
        }
      }
  `]
})
export class ViewCryptosComponent implements OnInit {
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private cryptoService = inject(CryptoService);
  orderOptions: any[] = [
    'ASC',
    'DESC'
  ];
  dateOptions: any[] = [
    '1 dia',
    '7 dias',
    '1 mês',
    '3 meses',
    '6 meses',
    '1 ano'
  ]
  cryptos: ViewCrypto[] = [];
  page = 1;
  pageSize = 10;
  cryptoCurrency = '';
  sortOrder = 'asc';
  searchValue = '';
  constructor() { }
  ngOnInit(): void {
    this.listCryptos();
  }
  createCrypto(): void {
    const dialogRef = this.dialog.open(CreateCryptoComponent, {
      width: '400px',
      height: '300px',
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('The dialog was closed');
      console.log(result);
    }
    );
  }
  cryptoDashboard(id: number) {
    this.router.navigate(['/crypto-dashboard', id]);
  }
  search(event: any) { }

  listCryptos() {
    this.cryptoService.listCryptos(
      this.page,
      this.pageSize,
      '',
      this.searchValue,
      '',
      this.sortOrder
    )
      .pipe(take(1))
      .subscribe({
        next: (cryptos) => {
          this.cryptos = cryptos.items;
        },
        error: (error) => { },
      }
      )
  }
}
