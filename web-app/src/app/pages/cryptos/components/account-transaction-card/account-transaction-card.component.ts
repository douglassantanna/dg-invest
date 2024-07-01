import { Component, Input, input } from '@angular/core';
import { AccountTransaction, AccountTransactionType } from '../../containers/account/account.component';
import { CommonModule, KeyValuePipe } from '@angular/common';

@Component({
  selector: 'app-account-transaction-card',
  standalone: true,
  imports: [CommonModule, KeyValuePipe],
  templateUrl: './account-transaction-card.component.html',
  styleUrl: './account-transaction-card.component.scss'
})
export class AccountTransactionCardComponent {
  objectKeys = Object.keys;
  @Input() groupedTransactions: { [date: string]: AccountTransaction[] } = {}
  transactions = input<AccountTransaction[]>([]);
  AccountTransactionType = AccountTransactionType;

  getTransactionTypeLabel(transactionType: AccountTransactionType): string {
    switch (transactionType) {
      case AccountTransactionType.DepositFiat:
        return 'Deposit Fiat';
      case AccountTransactionType.DepositCrypto:
        return 'Deposit Crypto';
      case AccountTransactionType.WithdrawToBank:
        return 'Bank Withdraw';
      case AccountTransactionType.In:
        return 'Money In';
      case AccountTransactionType.Out:
        return 'Money Out';
      default:
        return 'Unknown';
    }
  }

  isIncoming(transactionType: AccountTransactionType): boolean {
    return transactionType === AccountTransactionType.DepositFiat ||
      transactionType === AccountTransactionType.DepositCrypto ||
      transactionType === AccountTransactionType.In;
  }

  isOutgoing(transactionType: AccountTransactionType): boolean {
    return transactionType === AccountTransactionType.WithdrawToBank ||
      transactionType === AccountTransactionType.Out;
  }

  getTransactionSign(transactionType: AccountTransactionType): string {
    return this.isIncoming(transactionType) ? '+' : '-';
  }
}
