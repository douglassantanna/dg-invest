import { Component, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-transaction-table',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    PercentDifferenceComponent,
    FormatCurrencyPipe],
  templateUrl: './transaction-table.component.html',
})
export class TransactionTableComponent {
  orderOptions: any[] = [
    'Amount (Low to High)',
    'Amount (High to Low)',
    'Price Unit (Low to High)',
    'Price Unit (High to Low)',
    'Order Type (Buy First)',
    'Order Type (Sell First)',
    'Exchange Name (A-Z)',
    'Exchange Name (Z-A)'
  ];
  transactionDataLoading = input.required<boolean>();
  transactions = input<CryptoTransactionHistory[]>([]);
  transactionById(index: number, transaction: CryptoTransactionHistory) {
    return transaction.id;
  }
}
