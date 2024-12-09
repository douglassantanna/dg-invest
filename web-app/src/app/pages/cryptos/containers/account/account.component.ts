import { Component, OnInit, inject, signal } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CommonModule } from '@angular/common';
import { AccountTransactionCardComponent } from '../../components/account-transaction-card/account-transaction-card.component';
import { RouterModule } from '@angular/router';
import { AccountDto, AccountTransactionDto, GroupedAccountTransactionsDto, UserDto, UserService } from 'src/app/core/services/user.service';
import { ModalComponent } from 'src/app/layout/modal/modal.component';
import { DepositComponent } from '../deposit/deposit.component';
export type AccountTransaction = {
  imageUrl: string;
  transactionType: AccountTransactionType;
  transactionValue: number;
  date: Date;
  notes: string;
  cryptoAmount?: number;
  cryptoCurrentPrice?: number;
  cryptoSymbol?: string;
}

export enum AccountTransactionType {
  DepositFiat = 1,
  DepositCrypto = 2,
  WithdrawToBank = 3,
  In = 4,
  Out = 5
}
@Component({
  selector: 'app-account',
  standalone: true,
  imports: [
    CryptoFilterComponent,
    CommonModule,
    AccountTransactionCardComponent,
    RouterModule,
    ModalComponent,
    DepositComponent
  ],
  templateUrl: './account.component.html',
})
export class AccountComponent implements OnInit {
  private userService = inject(UserService);

  isDepositModalOpen = signal<boolean>(false);
  account = signal<AccountDto>({} as AccountDto);
  ngOnInit(): void {
    this.userService.getUserById().subscribe({
      next: (result) => {
        if (result) {
          const user = result.data as UserDto
          this.account.set(user.account!);
        }
      },
      error: (err) => { console.log(err) }
    })
  }

  searchTransactions(input: any) {
    console.log(input);
  }

  toggleDepositModal() {
    this.isDepositModalOpen.set(!this.isDepositModalOpen());
    console.log('account', this.account());

  }

  depositEvent(deposit: any) {
    if (deposit) {
      const accountBalance = this.account().balance + deposit.amount;
      this.account().balance = accountBalance;

      const depositDate = new Date(deposit.date);
      depositDate.setHours(0, 0, 0, 0);

      const existingTransactions = this.account().groupedAccountTransactions.filter((t) => {
        const transactionDate = new Date(t.date);
        transactionDate.setHours(0, 0, 0, 0);
        return transactionDate.getTime() === depositDate.getTime();
      });

      const transactionDto: AccountTransactionDto = {
        date: deposit.date,
        transactionType: deposit.accountTransactionType,
        amount: deposit.amount,
        exchangeName: deposit.exchangeName,
        currency: '',
        destination: '',
        notes: deposit.notes,
        cryptoCurrentPrice: deposit.currentPrice,
        cryptoSymbol: deposit.cryptoAssetId
      }

      if (existingTransactions.length > 0) {
        existingTransactions[0].transactions.push(transactionDto);
      } else {
        const groupedTransaction: GroupedAccountTransactionsDto = {
          date: deposit.date,
          transactions: [transactionDto]
        };
        const allTransactions = this.account().groupedAccountTransactions;
        allTransactions.unshift(groupedTransaction);
        const sortedTransactions = allTransactions.sort((a, b) => {
          return new Date(b.date).getTime() - new Date(a.date).getTime();
        })
        this.account().groupedAccountTransactions = sortedTransactions;
      }
    }
    this.toggleDepositModal();
  }
}
