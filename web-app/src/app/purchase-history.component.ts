import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';

@Component({
  selector: 'app-purchase-history',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule],
  template: `
    <table mat-table [dataSource]="dataSource">
      <ng-container matColumnDef="amount">
        <th mat-header-cell *matHeaderCellDef> Amount </th>
        <td mat-cell *matCellDef="let element"> {{element.amount}} </td>
      </ng-container>

      <ng-container matColumnDef="priceUnit">
        <th mat-header-cell *matHeaderCellDef> Price Unit </th>
        <td mat-cell *matCellDef="let element"> {{element.priceUnit}} </td>
      </ng-container>

      <ng-container matColumnDef="transaction">
        <th mat-header-cell *matHeaderCellDef> Transaction </th>
        <td mat-cell *matCellDef="let element"> {{element.transaction}} </td>
      </ng-container>

      <ng-container matColumnDef="date">
        <th mat-header-cell *matHeaderCellDef> Date </th>
        <td mat-cell *matCellDef="let element"> {{element.date | date:'dd/MM/y'}} </td>
      </ng-container>

      <ng-container matColumnDef="exchange">
        <th mat-header-cell *matHeaderCellDef> Exchange </th>
        <td mat-cell *matCellDef="let element"> {{element.exchange}} </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>
  `,
  styles: [`
  `]
})
export class PurchaseHistoryComponent {
  displayedColumns = ['amount', 'priceUnit', 'transaction', 'date', 'exchange'];
  dataSource: any[] = [
    {
      amount: 1.2,
      priceUnit: 420,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },
    {
      amount: 0.15,
      priceUnit: 2.58,
      date: Date.now(),
      exchange: 'Binance',
      transaction: 'buy'
    },

  ];
}
