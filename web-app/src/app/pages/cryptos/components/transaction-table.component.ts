import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { BehaviorSubject } from 'rxjs';
import { PercentDifferenceComponent } from './percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

@Component({
  selector: 'app-transaction-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PercentDifferenceComponent,
    FormatCurrencyPipe],
  template: `
 <div class="transaction-table-container">
  <header class="table-header">
    <h1>Purchase history</h1>
    <div class="form-group">
      <label for="filterByDate">Filter by date</label>
      <select id="filterByDate">
        <option *ngFor="let option of dateOptions" [value]="option">{{ option }}</option>
      </select>
    </div>
  </header>

  <div class="table-responsive">
    <table class="custom-table">
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
          <td>{{ element.price | formatCurrency }}</td>
          <td>
            <app-percent-difference
              *ngIf="element.transactionType === 1"
              [percentDifference]="element.percentDifference"></app-percent-difference>
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
  .transaction-table-container {
  border: 1px solid #ccc;
  border-radius: 8px;
  padding: 16px;
  background-color: #f9f9f9;
}

.table-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 16px;
  border-bottom: 1px solid #ccc;
  margin-bottom: 16px;
}

.table-header h1 {
  margin: 0;
  font-size: 24px;
}

.form-group {
  display: flex;
  flex-direction: column;
}

.form-group label {
  margin-bottom: 8px;
  font-weight: bold;
}

.form-group select {
  padding: 8px;
  font-size: 16px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.table-responsive {
  overflow-x: auto;
}

.custom-table {
  width: 100%;
  border-collapse: collapse;
  margin: 16px 0;
  border: 1px solid #ccc;
  border-radius: 8px;
}

.custom-table th, .custom-table td {
  border: 1px solid #ccc;
  padding: 8px;
  text-align: left;
}

.custom-table th {
  background-color: #f4f4f4;
  font-weight: bold;
}

.custom-table tr:nth-child(even) {
  background-color: #f9f9f9;
}
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
