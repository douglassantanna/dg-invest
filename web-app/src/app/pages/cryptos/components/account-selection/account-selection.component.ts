import { CurrencyPipe, NgClass } from '@angular/common';
import { Component, inject, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService, CreateAccountCommand, SelectAccountRequest } from 'src/app/core/services/account.service';

export interface SimpleAccountDto {
  subaccountTag: string;
  balance: number;
  id: number;
  isSelected: boolean;
}

@Component({
  selector: 'app-account-selection',
  templateUrl: './account-selection.component.html',
  standalone: true,
  imports: [
    CurrencyPipe,
    NgClass,
    FormsModule
  ]
})
export class AccountSelectionComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  accounts = signal<SimpleAccountDto[]>([]);
  accountCreatedEvent = output<SimpleAccountDto[]>();
  selectedAccount = this.accounts()[0];
  showNewAccountInput = signal(false);
  newAccountName = signal('');
  errorMessage = signal('');
  loading = signal(false);
  loadingAccounts = signal(false);

  ngOnInit(): void {
    this.loadAccounts();
  }

  selectAccount(account: SimpleAccountDto) {
    const command = { accountId: account.id } as SelectAccountRequest;
    this.accountService.selectAccount(command).subscribe({
      next: () => {
        this.updateAccountState(account);
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  toggleNewAccountInput() {
    this.showNewAccountInput.set(!this.showNewAccountInput());
    this.errorMessage.set('');
  }

  saveNewAccount() {
    this.loading.set(true);
    const duplicate = this.accounts().some(account => account.subaccountTag.toLowerCase() === this.newAccountName().trim().toLowerCase());
    if (duplicate) {
      this.loading.set(false);
      this.errorMessage.set('Account name already exists.');
    } else {
      let command = { subaccountTag: this.newAccountName() } as CreateAccountCommand;
      this.accountService.createAccount(command).subscribe({
        next: () => {
          this.loading.set(false);
          this.accounts().push({ subaccountTag: this.newAccountName(), balance: 0, id: 0, isSelected: false });
          this.newAccountName.set('');
          this.showNewAccountInput.set(false);
          this.errorMessage.set('');
        },
        error: (err) => {
          this.errorMessage.set(`Error creating account. ${err}`);
          this.loading.set(false);
        }
      });
    }
  }

  private updateAccountState(account: SimpleAccountDto) {
    this.accounts().forEach((acc) => {
      if (acc.id === account.id) {
        acc.isSelected = true;
      }
      else {
        acc.isSelected = false;
      }
    });
  }

  private loadAccounts() {
    this.loadingAccounts.set(true);
    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        this.loadingAccounts.set(false);
        this.accounts.set(accounts);
      },
      error: (err) => {
        this.loadingAccounts.set(false);
        console.error(err);
      }
    });
  }
}
