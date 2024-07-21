import { Pipe, PipeTransform } from '@angular/core';
import { AccountTransactionType } from '../models/deposit-fund-command';

@Pipe({
  name: 'transactionTypeLabel',
  standalone: true
})
export class TransactionTypeLabelPipe implements PipeTransform {
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

  transform(value: AccountTransactionType): string {
    const label = this.getTransactionTypeLabel(value);
    return label.split(' ').map(word => word[0]).join('');
  }
}
