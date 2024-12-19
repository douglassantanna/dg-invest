import { CurrencyPipe, NgClass } from '@angular/common';
import { Component, inject, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountService } from 'src/app/core/services/account.service';

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

  ngOnInit(): void {
    this.accountService.getAccounts().subscribe((accounts) => {
      this.accounts.set(accounts);
    })
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
      const duplicate = this.accounts().some(account => account.subaccountTag.toLowerCase() === this.newAccountName().trim().toLowerCase());
      if (duplicate) {
        this.errorMessage.set('Account name already exists.');
      } else {
        this.accounts().push({ subaccountTag: this.newAccountName(), balance: 0, id: 0 });
        this.newAccountName.set('');
        this.showNewAccountInput.set(false);
        this.errorMessage.set('');

      }
    }
  }
}
