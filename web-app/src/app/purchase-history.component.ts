import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';

import { CryptoDto } from './services/crypto.service';

@Component({
  selector: 'app-purchase-history',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatFormFieldModule,
    FormsModule,
    MatSelectModule,
    MatIconModule],
  template: `
  <header style="display: flex; justify-content:space-between;align-items:center;">
    <h1>Purchase history</h1>
    <mat-form-field appearance="outline">
    <mat-label>Filter by date</mat-label>
    <mat-select>
      <mat-option *ngFor="let option of dateOptions" [value]="option">
        {{option}}
      </mat-option>
    </mat-select>
  </mat-form-field>
  </header>
    <table mat-table [dataSource]="crypto.transactions">
      <ng-container matColumnDef="amount">
        <th mat-header-cell *matHeaderCellDef> Amount </th>
        <td mat-cell *matCellDef="let element"> {{element.amount}} </td>
      </ng-container>

      <ng-container matColumnDef="priceUnit">
        <th mat-header-cell *matHeaderCellDef> Price Unit </th>
        <td mat-cell *matCellDef="let element"> {{element.price}} </td>
      </ng-container>

      <ng-container matColumnDef="transaction">
        <th mat-header-cell *matHeaderCellDef> Transaction </th>
        <td mat-cell *matCellDef="let element">
          <ng-container *ngIf="element.transactionType === 1">
            <mat-icon style="color: #32CD32;">add</mat-icon>
          </ng-container>
          <ng-container *ngIf="element.transactionType === 2">
            <mat-icon style="color: #FF5733;">remove</mat-icon>
          </ng-container>
        </td>
      </ng-container>


      <ng-container matColumnDef="date">
        <th mat-header-cell *matHeaderCellDef> Date </th>
        <td mat-cell *matCellDef="let element"> {{element.purchaseDate | date:'dd/MM/y'}} </td>
      </ng-container>

      <ng-container matColumnDef="exchange">
        <th mat-header-cell *matHeaderCellDef> Exchange </th>
        <td mat-cell *matCellDef="let element"> {{element.exchangeName}} </td>
      </ng-container>

      <tr mat-header-row *matHeaderRowDef="displayedColumns; sticky: true"></tr>
      <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
    </table>
  `,
  styles: [`
  `]
})
export class PurchaseHistoryComponent {
  @Input() crypto!: CryptoDto;

  displayedColumns = ['amount', 'priceUnit', 'transaction', 'date', 'exchange'];
  dataSource: any[] = [];
  dateOptions: any[] = [
    '1 dia',
    '7 dias',
    '1 mês',
    '3 meses',
    '6 meses',
    '1 ano'
  ]
}
