import { CurrencyPipe, NgClass } from '@angular/common';
import { Component, inject, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService, CreateAccountCommand } from 'src/app/core/services/account.service';

export interface SimpleAccountDto {
  subaccountTag: string;
  balance: number;
  id: number;
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
  accountCreatedEvent = output();
  selectedAccount = this.accounts()[0];
  showNewAccountInput = signal(false);
  newAccountName = signal('');
  errorMessage = signal('');
  loading = signal(false);
  ngOnInit(): void {
    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  selectAccount(account: SimpleAccountDto) {
    this.selectedAccount = account;
  }

  toggleNewAccountInput() {
    this.showNewAccountInput.set(!this.showNewAccountInput());
    this.errorMessage.set('');
  }

  saveNewAccount() {
    if (this.newAccountName().trim()) {
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
            this.accounts().push({ subaccountTag: this.newAccountName(), balance: 0, id: 0 });
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
  }
}
