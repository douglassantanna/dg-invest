import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { BehaviorSubject } from 'rxjs';
import { PercentDifferenceComponent } from './percent-difference.component';

@Component({
  selector: 'app-transaction-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PercentDifferenceComponent],
  template: `
  <div class="border border-1 rounded">
    <header class="d-flex justify-content-between align-items-center p-2">
      <h1>Purchase history</h1>
      <div class="form-group">
        <label for="filterByDate">Filter by date</label>
        <select class="form-control" id="filterByDate">
          <option *ngFor="let option of dateOptions" [value]="option">{{ option }}</option>
        </select>
      </div>
    </header>

    <div class="table-responsive">
      <table class="table table-bordered">
        <thead>
          <tr>
            <th>Amount</th>
            <th>Price Unit</th>
            <th>%</th>
            <th>Transaction</th>
            <th>Date</th>
            <th>Exchange</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let element of transactions | async trackBy: transactionById">
            <td>{{ element.amount }}</td>
            <td>{{ element.price | currency: 'USD':'symbol':'1.2-2' }}</td>
            <td>
              <app-percent-difference
                *ngIf="element.transactionType === 1"
                [percentDifference]="element.percentDifference"/>
            </td>
            <td>
              <span *ngIf="element.transactionType === 1">🛒 Buy</span>
              <span *ngIf="element.transactionType === 2">💰 Sell</span>
            </td>
            <td>{{ element.purchaseDate | date: 'dd/MM/y' }}</td>
            <td>{{ element.exchangeName }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
  `,
  styles: [`
  `]
})
export class TransactionTableComponent {
  dateOptions: any[] = [
    '1 dia',
    '7 dias',
    '1 mês',
    '3 meses',
    '6 meses',
    '1 ano'
  ]
  @Input() transactions: BehaviorSubject<CryptoTransactionHistory[]> = new BehaviorSubject<CryptoTransactionHistory[]>([]);
  transactionById(index: number, transaction: CryptoTransactionHistory) {
    return transaction.id;
  }
}
