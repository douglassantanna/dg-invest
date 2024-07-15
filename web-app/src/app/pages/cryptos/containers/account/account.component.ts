import { Component, OnInit, inject, signal } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter.component';
import { CommonModule } from '@angular/common';
import { AccountTransactionCardComponent } from '../../components/account-transaction-card/account-transaction-card.component';
import { RouterModule } from '@angular/router';
import { AccountDto, UserDto, UserService } from 'src/app/core/services/user.service';
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
    RouterModule
  ],
  templateUrl: './account.component.html',
})
export class AccountComponent implements OnInit {
  private userService = inject(UserService);
  account = signal<AccountDto>({} as AccountDto);
  ngOnInit(): void {
    this.userService.getUserById().subscribe({
      next: (result) => {
        if (result) {
          const user = result.data as UserDto
          console.log(user.account);
          this.account.set(user.account!);
        }
      },
      error: (err) => { console.log(err) }
    })
  }
  recentTransactions: { [date: string]: AccountTransaction[] } = {
    "2024-06-25": [
      {
        imageUrl: 'assets/bitcoin-symbol.png',
        transactionType: AccountTransactionType.DepositFiat,
        transactionValue: 1000,
        date: new Date(),
        notes: 'Deposit fiat money to account',
      },
      {
        imageUrl: 'assets/bitcoin-symbol.png',
        transactionType: AccountTransactionType.DepositCrypto,
        transactionValue: 500,
        date: new Date(),
        cryptoSymbol: 'btc',
        notes: 'Deposit crypto to account',
        cryptoAmount: 0.05,
        cryptoCurrentPrice: 10000,
      },
      {
        imageUrl: 'assets/bitcoin-symbol.png',
        transactionType: AccountTransactionType.WithdrawToBank,
        transactionValue: 20000000,
        date: new Date(),
        notes: 'Withdraw fiat money to bank',
      },
      {
        imageUrl: 'assets/bitcoin-symbol.png',
        transactionType: AccountTransactionType.In,
        transactionValue: 40230,
        date: new Date(),
        cryptoSymbol: 'btc',
        notes: 'Crypto sold, money received to account',
        cryptoAmount: 0.04,
        cryptoCurrentPrice: 61500,
      },
      {
        imageUrl: 'assets/bitcoin-symbol.png',
        transactionType: AccountTransactionType.Out,
        transactionValue: 250,
        date: new Date(),
        cryptoSymbol: 'btc',
        notes: 'Money used to buy crypto',
        cryptoAmount: 0.025,
        cryptoCurrentPrice: 10000,
      },
    ]
  };
  searchTransactions(input: any) {
    console.log(input);
  }
}
