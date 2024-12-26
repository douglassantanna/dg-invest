import { Component, OnInit, inject, signal } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CommonModule } from '@angular/common';
import { AccountTransactionCardComponent } from '../../components/account-transaction-card/account-transaction-card.component';
import { RouterModule } from '@angular/router';
import { AccountDto, AccountTransactionDto, GroupedAccountTransactionsDto } from 'src/app/core/services/user.service';
import { ModalComponent } from 'src/app/layout/modal/modal.component';
import { DepositComponent } from '../deposit/deposit.component';
import { DepositFundCommand, WithdrawFundCommand } from 'src/app/core/models/deposit-fund-command';
import { WithdrawComponent } from '../withdraw/withdraw.component';
import { AccountService } from 'src/app/core/services/account.service';
export type AccountTransaction = {
  imageUrl: string;
  transactionType: AccountTransactionType;
  transactionValue: number;
  date: Date;
  notes: string;
  cryptoAmount?: number;
  cryptoCurrentPrice?: number;
  cryptoSymbol?: string;
  fee?: number;
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
    DepositComponent,
    WithdrawComponent
  ],
  templateUrl: './account.component.html',
})
export class AccountComponent implements OnInit {
  private accountService = inject(AccountService);

  isDepositModalOpen = signal<boolean>(false);
  isWithdrawModalOpen = signal<boolean>(false);
  account = signal<AccountDto>({} as AccountDto);
  ngOnInit(): void {
    this.accountService.getSelectedAccount().subscribe({
      next: (result) => {
        if (result) {
          this.account.set(result);
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
  }

  toggleWithdrawModal() {
    this.isWithdrawModalOpen.set(!this.isWithdrawModalOpen());
  }

  depositEvent(deposit: DepositFundCommand | null) {
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
        cryptoSymbol: deposit.cryptoAssetId,
        fee: 0
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

  withdrawEvent(withdraw: WithdrawFundCommand | null) {
    if (!withdraw) this.toggleWithdrawModal();
  }
}
