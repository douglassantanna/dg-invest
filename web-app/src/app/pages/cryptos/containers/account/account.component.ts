import { Component, OnInit, inject, signal } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
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
          this.account.set(user.account!);
        }
      },
      error: (err) => { console.log(err) }
    })
  }

  searchTransactions(input: any) {
    console.log(input);
  }
}
