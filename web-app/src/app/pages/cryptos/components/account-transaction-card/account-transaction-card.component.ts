import { TransactionTypeLabelPipe } from './../../../../core/pipes/transaction-type-label.pipe';
import { Component, Input, input } from '@angular/core';
import { AccountTransaction, AccountTransactionType } from '../../containers/account/account.component';
import { AccountTransactionDto, GroupedAccountTransactionsDto } from 'src/app/core/services/user.service';
import { CryptoSymbolPipe } from 'src/app/core/pipes/crypto-symbol.pipe';
import { CurrencyPipe, DatePipe, NgClass, UpperCasePipe } from '@angular/common';

@Component({
  selector: 'app-account-transaction-card',
  standalone: true,
  imports: [
    DatePipe,
    NgClass,
    CurrencyPipe,
    UpperCasePipe,
    CryptoSymbolPipe,
    TransactionTypeLabelPipe],
  templateUrl: './account-transaction-card.component.html',
})
export class AccountTransactionCardComponent {
  objectKeys = Object.keys;
  groupedTransactionsNew = input<GroupedAccountTransactionsDto[]>([]);

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

  getTransactionTypeClass(transactionType: AccountTransactionType): string {
    switch (transactionType) {
      case AccountTransactionType.DepositFiat:
        return 'deposit-fiat';
      case AccountTransactionType.DepositCrypto:
        return 'deposit-crypto';
      case AccountTransactionType.WithdrawToBank:
        return 'bank-withdraw';
      case AccountTransactionType.In:
        return 'money-in';
      case AccountTransactionType.Out:
        return 'money-out';
      default:
        return 'unknown';
    }
  }

  getTransactionValue(accountTransaction: AccountTransactionDto): number {
    return accountTransaction.amount * accountTransaction.cryptoCurrentPrice;
  }

  isCryptoTransaction(accountTransaction: AccountTransactionType): boolean {
    return accountTransaction === AccountTransactionType.DepositCrypto
      || accountTransaction === AccountTransactionType.In
      || accountTransaction === AccountTransactionType.Out;
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
