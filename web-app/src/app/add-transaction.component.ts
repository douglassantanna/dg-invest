import { CommonModule } from '@angular/common';
import { Component, inject, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { CreateTransactionCommand, CryptoService, Transaction } from './services/crypto.service';

@Component({
  selector: 'app-add-transaction',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressSpinnerModule],
  template: `
      <mat-card>
        <h1>{{title}}</h1>
        <mat-card-content>
          <form (ngSubmit)="save()" #transactionForm="ngForm">
            <div class="fields">
              <mat-form-field appearance="outline">
                <mat-label>Amount</mat-label>
                <input required matInput type="number" name="amount" [(ngModel)]="transaction.amount">
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Price per Unit</mat-label>
                <input matInput type="number" name="price" [(ngModel)]="transaction.price">
              </mat-form-field>
            </div>

            <div class="fields">
              <mat-form-field appearance="outline">
                <mat-label>Date of Purchase</mat-label>
                <input matInput [matDatepicker]="picker" [(ngModel)]="transaction.purchaseDate" name="purchaseDate">
                <mat-datepicker-toggle matSuffix [for]="picker"></mat-datepicker-toggle>
                <mat-datepicker #picker></mat-datepicker>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Exchange Name</mat-label>
                <input matInput type="text" name="exchangeName" [(ngModel)]="transaction.exchangeName">
              </mat-form-field>
            </div>

            <div class="fields">
              <mat-form-field appearance="outline">
                <mat-label>Transcation type</mat-label>
                <mat-select [(ngModel)]="transaction.transactionType" name="transactionType">
                  <mat-option [value]="'1'">Buy</mat-option>
                  <mat-option [value]="'1'">Sell</mat-option>
                </mat-select>
              </mat-form-field>
            </div>
          </form>
        </mat-card-content>
        <mat-card-actions align="end">
          <button [disabled]="isLoading" mat-raised-button color="warn">Reset</button>
          <ng-container *ngIf="!isLoading;else loading">
            <button [disabled]="!transaction" mat-raised-button (click)="transactionForm.invalid" color="primary" type="submit">Save</button>
          </ng-container>
        </mat-card-actions>
      </mat-card>

      <ng-template #loading>
      <button mat-raised-button color="primary" type="button">
        <mat-spinner diameter="20" color="accent"></mat-spinner>
      </button>
      </ng-template>
  `,
  styles: [`
    h1{
      padding:16px 0px 0px 16px
    }
    mat-form-field{
      width: 100%;
    }
    .fields{
      display:flex;
      gap:10px;
    }
    button{
      margin-left:10px;
    }
    @media (max-width: 640px) {
        .fields{
          display:flex;
          flex-direction:column;
        }
      }
  `]
})
export class AddTransactionComponent {
  @Input() cryptoAssetId!: number;
  transaction: Transaction = {} as Transaction;
  isLoading = false;
  private cryptoService = inject(CryptoService);

  title = 'Add transaction';
  cryptoOptions: any[] = [
    'Bitcoin',
    'Ethereum',
    'Tether',
    'Litecoin',
    'Cardano',
    'Binance Coin',
    'Polkadot',
    'Solana',
    'Avalanche',
  ];

  save() {
    let command = {
      amount: this.transaction.amount,
      price: this.transaction.price,
      purchaseDate: this.transaction.purchaseDate,
      exchangeName: this.transaction.exchangeName,
      transactionType: this.transaction.transactionType,
      cryptoAssedId: this.cryptoAssetId
    } as CreateTransactionCommand;
    this.isLoading = true;

    setTimeout(() => {
      this.isLoading = false;
    }, 5000);
    console.log(command)
    // this.cryptoService.createTransaction(command).subscribe({
    //   next: (data) => {
    //     this.isLoading = false;
    //     console.log(data);
    //   },
    //   error: (err) => {
    //     this.isLoading = false;
    //     console.log(err);
    //   },
    // })
  }
}
