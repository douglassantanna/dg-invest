import { computed, Injectable, signal } from '@angular/core';
import { AccountDto, AccountTransactionDto, GroupedAccountTransactionsDto } from './user.service';
import { AccountSelectorOption } from '../models/account-selector-option';
import { AccountTransactionType } from '../models/deposit-fund-command';

interface DemoAccount extends AccountSelectorOption {
  transactions: AccountTransactionDto[];
}

const transactionTypeLabels = new Map<AccountTransactionType, string>([
  [AccountTransactionType.DepositFiat, 'Deposit'],
  [AccountTransactionType.DepositCrypto, 'Deposit Crypto'],
  [AccountTransactionType.WithdrawToBank, 'Withdraw to Bank'],
  [AccountTransactionType.In, 'Sell'],
  [AccountTransactionType.Out, 'Buy'],
  [AccountTransactionType.WithdrawCrypto, 'Withdraw Crypto'],
  [AccountTransactionType.TransferIn, 'Transfer In'],
  [AccountTransactionType.TransferOut, 'Transfer Out'],
]);

@Injectable({ providedIn: 'root' })
export class AccountSelectorDemoStateService {
  private readonly demoAccounts = signal<DemoAccount[]>([
    this.createDemoAccount({
      accountId: 10,
      accountName: 'Personal portfolio',
      balance: 18265.43,
      isSelected: true,
      accountType: 'Manual',
      exchangeName: null,
      isBybitCombinedWallet: false,
      healthStatus: null,
      lastSyncAt: null,
      errorCount: 0,
      lastErrorMessage: null,
    }, 120),
    this.createDemoAccount({
      accountId: 11,
      accountName: 'Tax reserve',
      balance: 7200,
      isSelected: false,
      accountType: 'Manual',
      exchangeName: null,
      isBybitCombinedWallet: false,
      healthStatus: null,
      lastSyncAt: null,
      errorCount: 0,
      lastErrorMessage: null,
    }, 32),
    this.createDemoAccount({
      accountId: 101,
      accountName: 'Bybit Main',
      balance: 25842.1,
      isSelected: false,
      accountType: 'Exchange',
      exchangeName: 'Bybit',
      isBybitCombinedWallet: true,
      healthStatus: 'Connected',
      lastSyncAt: '2026-10-15T13:58:00Z',
      errorCount: 0,
      lastErrorMessage: null,
    }, 80),
    this.createDemoAccount({
      accountId: 102,
      accountName: 'Bybit Trading',
      balance: 8430.5,
      isSelected: false,
      accountType: 'Exchange',
      exchangeName: 'Bybit',
      isBybitCombinedWallet: false,
      healthStatus: 'NeedsAttention',
      lastSyncAt: '2026-10-15T13:45:00Z',
      errorCount: 2,
      lastErrorMessage: 'Temporary connection error. Saved account history is still available.',
    }, 120),
    this.createDemoAccount({
      accountId: 103,
      accountName: 'Bybit Funding',
      balance: 12040,
      isSelected: false,
      accountType: 'Exchange',
      exchangeName: 'Bybit',
      isBybitCombinedWallet: false,
      healthStatus: 'Paused',
      lastSyncAt: '2026-10-14T13:00:00Z',
      errorCount: 0,
      lastErrorMessage: null,
    }, 32),
    this.createDemoAccount({
      accountId: 201,
      accountName: 'Kraken Spot',
      balance: 4305.75,
      isSelected: false,
      accountType: 'Exchange',
      exchangeName: 'Kraken',
      isBybitCombinedWallet: false,
      healthStatus: 'Connected',
      lastSyncAt: '2026-10-15T13:52:00Z',
      errorCount: 0,
      lastErrorMessage: null,
    }, 32),
  ]);

  readonly accountOptions = computed(() => this.demoAccounts().map(({ transactions: _transactions, ...option }) => option));
  readonly selectedAccount = computed(() => this.demoAccounts().find(account => account.isSelected) ?? this.demoAccounts()[0]);

  selectAccount(accountId: number): boolean {
    if (!this.demoAccounts().some(account => account.accountId === accountId))
      return false;

    this.demoAccounts.update(accounts => accounts.map(account => ({
      ...account,
      isSelected: account.accountId === accountId,
    })));
    return true;
  }

  addManualPortfolio(name: string): { success: boolean; error?: string } {
    const normalizedName = name.trim();
    if (!normalizedName)
      return { success: false, error: 'Enter a portfolio name.' };

    if (this.demoAccounts().some(account => account.accountName.toLowerCase() === normalizedName.toLowerCase()))
      return { success: false, error: 'An account with this name already exists.' };

    const nextId = Math.max(...this.demoAccounts().map(account => account.accountId)) + 1;
    this.demoAccounts.update(accounts => [...accounts, this.createDemoAccount({
      accountId: nextId,
      accountName: normalizedName,
      balance: 0,
      isSelected: false,
      accountType: 'Manual',
      exchangeName: null,
      isBybitCombinedWallet: false,
      healthStatus: null,
      lastSyncAt: null,
      errorCount: 0,
      lastErrorMessage: null,
    }, 0)]);

    return { success: true };
  }

  getSelectedAccountDetails(page: number, pageSize: number, filter: string, startDate?: string, endDate?: string, status?: string): AccountDto {
    const selected = this.selectedAccount();
    const normalizedFilter = filter.trim().toLowerCase();
    const filteredTransactions = selected.transactions
      .filter(transaction => !normalizedFilter
        || transaction.exchangeName.toLowerCase().includes(normalizedFilter)
        || transaction.notes.toLowerCase().includes(normalizedFilter)
        || transaction.cryptoSymbol.toLowerCase().includes(normalizedFilter)
        || (transactionTypeLabels.get(transaction.transactionType) ?? '').toLowerCase().includes(normalizedFilter))
      .filter(transaction => !startDate || this.dateKey(transaction.date) >= startDate)
      .filter(transaction => !endDate || this.dateKey(transaction.date) <= endDate)
      .filter(transaction => this.matchesStatus(transaction, status))
      .sort((left, right) => new Date(right.date).getTime() - new Date(left.date).getTime());

    const totalCount = filteredTransactions.length;
    const currentPage = Math.max(1, page);
    const pagedTransactions = filteredTransactions.slice((currentPage - 1) * pageSize, currentPage * pageSize);
    const transactionsByDate = new Map<string, AccountTransactionDto[]>();
    for (const transaction of pagedTransactions) {
      const date = this.dateKey(transaction.date);
      transactionsByDate.set(date, [...(transactionsByDate.get(date) ?? []), transaction]);
    }

    const groupedTransactions: GroupedAccountTransactionsDto[] = [...transactionsByDate.entries()].map(([date, transactions]) => ({
      date: `${date}T00:00:00.000Z`,
      transactions,
    }));

    return {
      id: selected.accountId,
      balance: selected.balance,
      groupedAccountTransactions: groupedTransactions,
      totalCount,
      page: currentPage,
      pageSize,
      hasNextPage: currentPage * pageSize < totalCount,
      hasPreviousPage: currentPage > 1,
      isBybitCombinedWallet: selected.isBybitCombinedWallet,
    };
  }

  private createDemoAccount(option: AccountSelectorOption, transactionCount: number): DemoAccount {
    const transactions = Array.from({ length: transactionCount }, (_, index) => this.createDemoTransaction(option, index));
    return { ...option, transactions };
  }

  private createDemoTransaction(account: AccountSelectorOption, index: number): AccountTransactionDto {
    const date = new Date(Date.UTC(2026, 9, 15 - Math.floor(index / 2), 15 - (index % 8), 20 - (index % 15)));
    const kind = index % 10;
    const transactionType = kind === 0
      ? AccountTransactionType.DepositFiat
      : kind === 1
        ? AccountTransactionType.TransferIn
        : kind === 2
          ? AccountTransactionType.In
          : kind === 3
            ? AccountTransactionType.WithdrawToBank
            : AccountTransactionType.Out;
    const cryptoSymbol = ['BTC', 'ETH', 'SOL', 'XRP'][index % 4];
    const status = index % 13 === 0 ? 'Failed' : index % 7 === 0 ? 'Pending' : 'Filled';
    const amount = transactionType === AccountTransactionType.DepositFiat ? 500 + index * 25 : 125 + index * 17;

    return {
      date: date.toISOString(),
      transactionType,
      amount,
      exchangeName: account.exchangeName ?? '',
      currency: 'USD',
      destination: '',
      notes: transactionType === AccountTransactionType.DepositFiat
        ? 'Monthly contribution'
        : `${cryptoSymbol} ${account.exchangeName ? 'exchange' : 'portfolio'} activity ${index + 1}`,
      cryptoCurrentPrice: cryptoSymbol === 'BTC' ? 64000 : cryptoSymbol === 'ETH' ? 3200 : 150,
      cryptoSymbol,
      fee: account.accountType === 'Exchange' ? 0.75 : 0,
      exchangeStatus: status,
      feeCurrency: account.exchangeName ? 'USDT' : 'USD',
      feeQuoteValue: account.accountType === 'Exchange' ? 0.75 : 0,
    };
  }

  private matchesStatus(transaction: AccountTransactionDto, status?: string): boolean {
    if (!status) return true;

    const exchangeStatus = (transaction.exchangeStatus ?? '').toLowerCase();
    switch (status.toLowerCase()) {
      case 'completed':
        return exchangeStatus === '3' || ['success', 'filled', 'internaltransfer'].includes(exchangeStatus);
      case 'failed':
        return exchangeStatus === '4' || ['failed', 'fail'].includes(exchangeStatus);
      case 'pending':
        return exchangeStatus !== '3' && exchangeStatus !== '4'
          && !['success', 'filled', 'internaltransfer', 'failed', 'fail'].includes(exchangeStatus);
      default:
        return true;
    }
  }

  private dateKey(date: string | Date): string {
    return new Date(date).toISOString().slice(0, 10);
  }
}
