import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountTransactionCardComponent } from '../../components/account-transaction-card/account-transaction-card.component';
import { RouterModule } from '@angular/router';
import { AccountDto } from 'src/app/core/services/user.service';
import { AccountSelectorOption, AccountHealthStatus } from 'src/app/core/models/account-selector-option';
import { AccountSelectorStateService } from 'src/app/core/services/account-selector-state.service';
import { AccountService } from 'src/app/core/services/account.service';
import { CryptoService } from 'src/app/core/services/crypto.service';
@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CryptoFilterComponent, CommonModule, FormsModule, AccountTransactionCardComponent, RouterModule],
  templateUrl: './account.component.html',
})
export class AccountComponent implements OnInit {
  private readonly accountState = inject(AccountSelectorStateService);
  private readonly accountService = inject(AccountService);
  private readonly cryptoService = inject(CryptoService);

  account = signal<AccountDto>({} as AccountDto);
  accountSelectorOptions = this.accountState.options;
  selectorLoading = this.accountState.isLoading;
  selectedAccountOption = this.accountState.selectedAccount;
  private selectionError = signal('');
  selectorError = computed(() => this.selectionError() || this.accountState.errorMessage());
  manualAccounts = computed(() => this.accountSelectorOptions().filter(option => option.accountType === 'Manual'));
  exchangeGroups = computed(() => {
    const exchanges = new Map<string, AccountSelectorOption[]>();
    for (const account of this.accountSelectorOptions().filter(option => option.accountType === 'Exchange')) {
      const exchangeName = account.exchangeName ?? 'Other exchange';
      exchanges.set(exchangeName, [...(exchanges.get(exchangeName) ?? []), account]);
    }
    return [...exchanges.entries()].map(([exchangeName, accounts]) => ({ exchangeName, accounts }));
  });
  currentPage = signal(1);
  pageSize = 20;
  currentFilter = signal('');
  startDate = signal<string>('');
  endDate = signal<string>('');
  currentStatus = signal<string>('');
  selectingAccountId = signal<number | null>(null);
  accountActivityError = signal('');
  isAddingManualAccount = signal(false);
  newAccountName = '';
  savingManualAccount = signal(false);
  createAccountError = signal('');

  ngOnInit(): void {
    this.accountState.load();
    this.loadAccountDetails();
  }

  loadAccountDetails(): void {
    this.accountService.getSelectedAccount(
      this.currentPage(),
      this.pageSize,
      this.currentFilter(),
      this.startDate() || undefined,
      this.endDate() || undefined,
      this.currentStatus() || undefined
    ).subscribe({
      next: result => {
        this.accountActivityError.set('');
        if (result) this.account.set(result);
      },
      error: () => this.accountActivityError.set('Could not load the selected account activity. Try again.'),
    });
  }

  searchTransactions(input: string): void {
    this.currentFilter.set(input);
    this.currentPage.set(1);
    this.loadAccountDetails();
  }

  onStartDateChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.startDate.set(input.value);
    this.currentPage.set(1);
    this.loadAccountDetails();
  }

  onEndDateChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.endDate.set(input.value);
    this.currentPage.set(1);
    this.loadAccountDetails();
  }

  clearDateFilters(): void {
    this.startDate.set('');
    this.endDate.set('');
    this.currentPage.set(1);
    this.loadAccountDetails();
  }

  onStatusChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.currentStatus.set(select.value);
    this.currentPage.set(1);
    this.loadAccountDetails();
  }

  nextPage(): void {
    if (!this.account().hasNextPage) return;
    this.currentPage.update((p) => p + 1);
    this.loadAccountDetails();
  }

  previousPage(): void {
    if (!this.account().hasPreviousPage) return;
    this.currentPage.update((p) => p - 1);
    this.loadAccountDetails();
  }

  totalPages(): number {
    return Math.ceil(this.account().totalCount / this.pageSize);
  }

  isSelected(account: AccountSelectorOption): boolean {
    return account.isSelected;
  }

  toggleAddManualAccount(): void {
    this.isAddingManualAccount.update(isAdding => !isAdding);
    this.newAccountName = '';
    this.createAccountError.set('');
  }

  createManualAccount(): void {
    const name = this.newAccountName.trim();
    if (!name || this.savingManualAccount()) return;

    if (this.manualAccounts().some(account => account.accountName.toLowerCase() === name.toLowerCase())) {
      this.createAccountError.set('An account with this name already exists.');
      return;
    }

    this.savingManualAccount.set(true);
    this.createAccountError.set('');
    this.accountState.createManualPortfolio({ name }).subscribe({
      next: () => {
        this.savingManualAccount.set(false);
        this.isAddingManualAccount.set(false);
        this.newAccountName = '';
      },
      error: error => {
        this.savingManualAccount.set(false);
        this.createAccountError.set(typeof error.error === 'string' ? error.error : 'Could not create the portfolio. Try another name.');
      },
    });
  }

  selectAccount(account: AccountSelectorOption): void {
    if (account.isSelected || this.selectingAccountId() !== null) return;

    this.selectingAccountId.set(account.accountId);
    this.selectionError.set('');
    this.accountState.selectAccount(account.accountId).subscribe({
      next: response => {
        this.selectingAccountId.set(null);
        if (response?.isSuccess === false) {
          this.selectionError.set(response.message || 'Could not change the active account.');
          return;
        }

        this.cryptoService.accountTag.set(account.accountName);
        this.currentPage.set(1);
        this.loadAccountDetails();
      },
      error: () => {
        this.selectingAccountId.set(null);
        this.selectionError.set('Could not change the active account. Try again.');
      },
    });
  }

  healthIcon(health: AccountHealthStatus | null): string {
    switch (health) {
      case 'Connected': return 'check_circle';
      case 'NeedsAttention': return 'error';
      case 'Paused': return 'pause_circle';
      case 'NotConfigured': return 'key_off';
      default: return '';
    }
  }

  healthLabel(health: AccountHealthStatus | null): string {
    switch (health) {
      case 'NeedsAttention': return 'Needs attention';
      case 'NotConfigured': return 'Setup needed';
      default: return health ?? '';
    }
  }

  healthClasses(health: AccountHealthStatus | null): string {
    switch (health) {
      case 'Connected': return 'bg-emerald-50 text-emerald-800 ring-emerald-200';
      case 'NeedsAttention': return 'bg-amber-50 text-amber-900 ring-amber-200';
      case 'Paused': return 'bg-slate-100 text-slate-700 ring-slate-200';
      case 'NotConfigured': return 'bg-blue-50 text-blue-800 ring-blue-200';
      default: return '';
    }
  }

  exchangeManagementLink(account: AccountSelectorOption): string[] {
    return account.exchangeName?.toLowerCase() === 'bybit'
      ? ['/exchanges', 'bybit', account.accountId.toString()]
      : ['/exchanges'];
  }
}
