import { Component, Input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CryptoTransactionHistory } from 'src/app/core/models/crypto-transaction-history';
import { BehaviorSubject } from 'rxjs';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

@Component({
  selector: 'app-transaction-table',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    PercentDifferenceComponent,
    FormatCurrencyPipe],
  templateUrl: 'transaction-table.component.html',
})
export class TransactionTableComponent {
  filterDisabled = signal(false);
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
