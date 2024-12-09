import { Component, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { BehaviorSubject } from 'rxjs';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';
import { AsyncPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-transaction-table',
  standalone: true,
  imports: [
    AsyncPipe,
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
  ]
  @Input() transactions: BehaviorSubject<CryptoTransactionHistory[]> = new BehaviorSubject<CryptoTransactionHistory[]>([]);
  transactionById(index: number, transaction: CryptoTransactionHistory) {
    return transaction.id;
  }
}
