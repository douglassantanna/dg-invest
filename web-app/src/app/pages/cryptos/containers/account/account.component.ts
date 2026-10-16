import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CryptoFilterComponent } from '../../components/crypto-filter/crypto-filter.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AccountTransactionCardComponent } from '../../components/account-transaction-card/account-transaction-card.component';
import { RouterModule } from '@angular/router';
import { ModalComponent } from 'src/app/layout/modal/modal.component';
import { DepositComponent } from '../deposit/deposit.component';
import { WithdrawComponent } from '../withdraw/withdraw.component';
import { DepositFundCommand, WithdrawFundCommand } from 'src/app/core/models/deposit-fund-command';
import { AccountDto } from 'src/app/core/services/user.service';
import { AccountSelectorOption, AccountHealthStatus } from 'src/app/core/models/account-selector-option';
import { AccountSelectorStateService } from 'src/app/core/services/account-selector-state.service';
import { AccountService } from 'src/app/core/services/account.service';
import { CryptoService } from 'src/app/core/services/crypto.service';
@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CryptoFilterComponent, CommonModule, FormsModule, AccountTransactionCardComponent, RouterModule, ModalComponent, DepositComponent, WithdrawComponent],
  templateUrl: './account.component.html',
})
export class AccountComponent implements OnInit {
  private readonly accountState = inject(AccountSelectorStateService);
  private readonly accountService = inject(AccountService);
  private readonly cryptoService = inject(CryptoService);
  private accountDetailsRequestId = 0;

  account = signal<AccountDto>({} as AccountDto);
  accountDetailsLoading = signal(false);
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
  isDepositModalOpen = signal(false);
  isWithdrawModalOpen = signal(false);

  ngOnInit(): void {
    this.accountState.load().subscribe({
      next: () => {
        const selected = this.selectedAccountOption();
        if (!selected) {
          this.accountActivityError.set('');
          return;
        }

        this.cryptoService.accountTag.set(selected.accountName);
        this.loadAccountDetails();
      },
      error: () => this.accountActivityError.set('Could not load accounts. Please try again.'),
    });
  }

  loadAccountDetails(): void {
    const selectedAccount = this.selectedAccountOption();
    if (!selectedAccount) {
      this.accountDetailsRequestId++;
      this.accountDetailsLoading.set(false);
      this.account.set({} as AccountDto);
      return;
    }

    const requestId = ++this.accountDetailsRequestId;
    this.accountDetailsLoading.set(true);
    this.accountActivityError.set('');
    this.account.set({} as AccountDto);

    this.accountService.getSelectedAccount(
      this.currentPage(),
      this.pageSize,
      this.currentFilter(),
      this.startDate() || undefined,
      this.endDate() || undefined,
      this.currentStatus() || undefined
    ).subscribe({
      next: result => {
        if (requestId !== this.accountDetailsRequestId || this.selectedAccountOption()?.accountId !== selectedAccount.accountId) return;
        this.accountActivityError.set('');
        if (result) this.account.set(result);
        this.accountDetailsLoading.set(false);
      },
      error: () => {
        if (requestId !== this.accountDetailsRequestId || this.selectedAccountOption()?.accountId !== selectedAccount.accountId) return;
        this.accountActivityError.set('Could not load the selected account activity. Try again.');
        this.accountDetailsLoading.set(false);
      },
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

    if (this.accountSelectorOptions().some(account => account.accountName.toLowerCase() === name.toLowerCase())) {
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
        const errorMessage = typeof error.error === 'string' ? error.error : error.error?.message;
        this.createAccountError.set(typeof errorMessage === 'string' && errorMessage.trim()
          ? errorMessage
          : 'Could not create the portfolio. Try another name.');
      },
    });
  }

  toggleDepositModal(): void {
    this.isDepositModalOpen.update(isOpen => !isOpen);
  }

  toggleWithdrawModal(): void {
    this.isWithdrawModalOpen.update(isOpen => !isOpen);
  }

  depositEvent(deposit: DepositFundCommand | null): void {
    this.isDepositModalOpen.set(false);
    if (deposit) this.refreshAccountAfterManualTransaction();
  }

  withdrawEvent(withdraw: WithdrawFundCommand | null): void {
    this.isWithdrawModalOpen.set(false);
    if (withdraw) this.refreshAccountAfterManualTransaction();
  }

  private refreshAccountAfterManualTransaction(): void {
    this.accountState.reload().subscribe({
      next: () => {
        const selected = this.selectedAccountOption();
        if (!selected) return;

        this.cryptoService.accountTag.set(selected.accountName);
        this.currentPage.set(1);
        this.loadAccountDetails();
      },
      error: () => {
        this.accountActivityError.set('Could not refresh the selected account after the transaction.');
        this.loadAccountDetails();
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
      case 'Disconnected': return 'link_off';
      default: return '';
    }
  }

  healthLabel(health: AccountHealthStatus | null): string {
    switch (health) {
      case 'NeedsAttention': return 'Needs attention';
      case 'NotConfigured': return 'Setup needed';
      case 'Disconnected': return 'Disconnected';
      default: return health ?? '';
    }
  }

  healthClasses(health: AccountHealthStatus | null): string {
    switch (health) {
      case 'Connected': return 'bg-emerald-50 text-emerald-800 ring-emerald-200';
      case 'NeedsAttention': return 'bg-amber-50 text-amber-900 ring-amber-200';
      case 'Paused': return 'bg-slate-100 text-slate-700 ring-slate-200';
      case 'NotConfigured': return 'bg-blue-50 text-blue-800 ring-blue-200';
      case 'Disconnected': return 'bg-red-50 text-red-800 ring-red-200';
      default: return '';
    }
  }

  exchangeManagementLink(account: AccountSelectorOption): string[] {
    return account.exchangeName?.toLowerCase() === 'bybit'
      ? ['/exchanges', 'bybit', account.accountId.toString()]
      : ['/exchanges'];
  }
}
