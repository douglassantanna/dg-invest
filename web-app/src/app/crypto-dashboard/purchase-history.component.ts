import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CryptoTransactionHistory } from './crypto-dashboard.component';

@Component({
  selector: 'app-purchase-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule],
  template: `

<div class="border border-1 rounded">
  <header class="d-flex justify-content-between align-items-center">
    <h1>Purchase history</h1>
    <div class="form-group">
      <label for="filterByDate">Filter by date</label>
      <select class="form-control" id="filterByDate">
        <option *ngFor="let option of dateOptions" [value]="option">{{ option }}</option>
      </select>
    </div>
  </header>

  <div class="table-responsive">
    <table class="table">
      <thead>
        <tr>
          <th>Amount</th>
          <th>Price Unit</th>
          <th>Transaction</th>
          <th>Date</th>
          <th>Exchange</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let element of transactionsHistory">
          <td>{{ element.amount }}</td>
          <td>{{ element.price | currency: 'USD':'symbol':'1.2-2' }}</td>
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


  <!-- <div class="border border-1 rounded">
    <header class="d-flex justify-content-between align-items-center">
      <h1>Purchase history</h1>
      <div class="form-group">
          <label for="filterByDate">Filter by date</label>
          <select class="form-control" id="filterByDate">
              <option *ngFor="let option of dateOptions" [value]="option">{{option}}</option>
          </select>
      </div>
    </header>

    <table class="table">
        <thead>
            <tr>
                <th>Amount</th>
                <th>Price Unit</th>
                <th>Transaction</th>
                <th>Date</th>
                <th>Exchange</th>
            </tr>
        </thead>
        <tbody>
            <tr *ngFor="let element of transactionsHistory">
                <td>{{element.amount}}</td>
                <td>{{element.price | currency:'USD':'symbol':'1.2-2'}}</td>
                <td>
                    <span *ngIf="element.transactionType === 1">
                        🛒 Buy
                    </span>
                    <span *ngIf="element.transactionType === 2">
                        💰 Sell
                    </span>
                </td>
                <td>{{element.purchaseDate | date:'dd/MM/y'}}</td>
                <td>{{element.exchangeName}}</td>
            </tr>
        </tbody>
    </table>
  </div> -->
  `,
  styles: [`
  `]
})
export class PurchaseHistoryComponent {
  dateOptions: any[] = [
    '1 dia',
    '7 dias',
    '1 mês',
    '3 meses',
    '6 meses',
    '1 ano'
  ]
  @Input() transactionsHistory: CryptoTransactionHistory[] = [];
}
