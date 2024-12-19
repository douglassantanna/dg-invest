import { CurrencyPipe, NgClass } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

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
export class AccountSelectionComponent {
  accounts = [
    { name: 'Main', balance: 1000 },
    { name: 'Dad', balance: 500 }
  ];
  selectedAccount = this.accounts[0];
  showNewAccountInput = false;
  newAccountName = '';
  errorMessage = '';
  selectAccount(account: any) {
    this.selectedAccount = account;
  }

  toggleNewAccountInput() {
    this.showNewAccountInput = !this.showNewAccountInput;
    this.errorMessage = '';
  }

  saveNewAccount() {
    if (this.newAccountName.trim()) {
      const duplicate = this.accounts.some(account => account.name.toLowerCase() === this.newAccountName.trim().toLowerCase());
      if (duplicate) {
        this.errorMessage = 'Account name already exists.';
      } else {
        this.accounts.push({ name: this.newAccountName, balance: 0 });
        this.newAccountName = '';
        this.showNewAccountInput = false;
        this.errorMessage = ''; // Clear error message on successful save
      }
    }
  }
}
