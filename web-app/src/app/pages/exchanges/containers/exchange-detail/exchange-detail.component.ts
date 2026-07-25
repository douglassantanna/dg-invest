import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ExchangeService } from 'src/app/core/services/exchange.service';
import { AccountService } from 'src/app/core/services/account.service';
import { ExchangeAccountDetailDto, ExchangeConnectionDto } from 'src/app/core/models/exchange-detail';
import { ExchangeTransactionDto } from 'src/app/core/models/exchange-transaction';
import { BybitSubMemberDto } from 'src/app/core/models/bybit-sub-member';
import { SyncLogEntry } from 'src/app/core/models/sync-log-entry';

@Component({
  selector: 'app-exchange-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './exchange-detail.component.html',
})
export class ExchangeDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private exchangeService = inject(ExchangeService);
  private accountService = inject(AccountService);

  accountId = 0;
  detail: ExchangeAccountDetailDto | null = null;
  connection: ExchangeConnectionDto | null = null;
  loading = true;

  // Credentials
  apiKey = '';
  apiSecret = '';
  webhookSecret = '';
  savingCredentials = false;
  deletingCredentials = false;
  credentialsMessage = '';
  credentialsError = false;

  // Transactions
  transactions: ExchangeTransactionDto[] = [];
  loadingTransactions = false;

  // Sub-accounts
  accounts: { id: number; tag: string }[] = [];
  subMembers: BybitSubMemberDto[] = [];
  loadingSubMembers = false;
  syncing = false;
  syncMessage = '';
  mappedAccountIds: Record<string, number> = {};

  // Sync logs
  syncLogs: SyncLogEntry[] = [];
  loadingLogs = false;
  logDate = '';

  // Reconciliation
  reconciliation: { bybitTotal: number; appTotal: number; drift: number } | null = null;
  loadingReconciliation = false;
  confirmReconcile = false;
  reconciling = false;

  Math = Math;

  ngOnInit(): void {
    this.accountId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadDetail();
    this.loadAccounts();
  }

  private loadDetail(): void {
    this.loading = true;
    this.exchangeService.getExchangeAccountDetail(this.accountId).subscribe({
      next: (res) => {
        this.detail = res.data ?? null;
        this.connection = this.detail?.connections?.[0] ?? null;
        this.loading = false;
        this.loadTransactions();
      },
      error: () => this.loading = false,
    });
  }

  private loadAccounts(): void {
    this.accountService.getAccounts().subscribe({
      next: (result) => {
        this.accounts = result.map((a) => ({ id: a.id, tag: a.subaccountTag }));
      },
    });
  }

  private loadDetail(): void {
    this.loading = true;
    this.exchangeService.getExchangeAccountDetail(this.accountId).subscribe({
      next: (res) => {
        this.detail = res.data ?? null;
        this.connection = this.detail?.connections?.[0] ?? null;
        this.loading = false;
        this.loadTransactions();
      },
      error: () => this.loading = false,
    });
  }

  saveCredentials(): void {
    if (!this.accountId) return;
    this.savingCredentials = true;
    this.credentialsMessage = '';
    this.credentialsError = false;
    this.exchangeService
      .saveBybitCredentials(this.accountId, this.apiKey, this.apiSecret, this.webhookSecret)
      .subscribe({
        next: (res) => {
          this.credentialsMessage = res.message;
          if (res.isSuccess) {
            this.apiKey = '';
            this.apiSecret = '';
            this.webhookSecret = '';
            this.loadDetail();
          }
          this.savingCredentials = false;
        },
        error: () => {
          this.credentialsMessage = 'Failed to save credentials';
          this.credentialsError = true;
          this.savingCredentials = false;
        },
      });
  }

  deleteCredentials(): void {
    if (!this.accountId) return;
    this.deletingCredentials = true;
    this.exchangeService.deleteCredentials(this.accountId).subscribe({
      next: () => {
        this.deletingCredentials = false;
        this.loadDetail();
      },
      error: () => this.deletingCredentials = false,
    });
  }

  syncAccounts(): void {
    this.syncing = true;
    this.syncMessage = '';
    this.exchangeService.syncBybitAccounts().subscribe({
      next: (res) => {
        this.syncMessage = res.message;
        this.syncing = false;
        this.loadSubMembers();
      },
      error: () => {
        this.syncMessage = 'Sync failed';
        this.syncing = false;
      },
    });
  }

  loadSubMembers(): void {
    this.loadingSubMembers = true;
    this.exchangeService.getBybitSubMembers().subscribe({
      next: (res) => {
        this.subMembers = res.data ?? [];
        this.loadingSubMembers = false;
      },
      error: () => this.loadingSubMembers = false,
    });
  }

  mapAccount(member: BybitSubMemberDto): void {
    const targetId = this.mappedAccountIds[member.uid];
    if (!targetId) return;
    this.exchangeService.mapBybitAccount(targetId, member.uid).subscribe({
      next: (res) => {
        if (res.isSuccess) {
          this.mappedAccountIds[member.uid] = 0;
          this.loadSubMembers();
        }
      },
    });
  }

  private loadTransactions(): void {
    this.loadingTransactions = true;
    this.exchangeService.getExchangeTransactions(this.accountId, 20).subscribe({
      next: (res) => {
        this.transactions = res.data ?? [];
        this.loadingTransactions = false;
      },
      error: () => this.loadingTransactions = false,
    });
  }

  fetchSyncLogs(): void {
    this.loadingLogs = true;
    this.exchangeService.getSyncLogs(this.accountId, this.logDate || undefined).subscribe({
      next: (res) => {
        this.syncLogs = res.data ?? [];
        this.loadingLogs = false;
      },
      error: () => this.loadingLogs = false,
    });
  }

  onLogDateChange(date: string): void {
    this.logDate = date;
    this.fetchSyncLogs();
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Connected': return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300';
      case 'Error': return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300';
      case 'Disconnected': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300';
    }
  }

  logStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Success': return 'bg-green-100 text-green-700';
      case 'Duplicate': return 'bg-yellow-100 text-yellow-700';
      case 'Failed': return 'bg-red-100 text-red-700';
      default: return 'bg-gray-100 text-gray-700';
    }
  }

  txStatusBadgeClass(status: string | null): string {
    switch (status) {
      case '3': return 'bg-green-100 text-green-700';
      case 'success': return 'bg-green-100 text-green-700';
      case '4': return 'bg-red-100 text-red-700';
      default: return 'bg-yellow-100 text-yellow-700';
    }
  }

  txStatusLabel(status: string | null): string {
    switch (status) {
      case '3': return 'Completed';
      case 'success': return 'Completed';
      case '4': return 'Failed';
      case '0': return 'Unknown';
      case '1': return 'Pending';
      case '2': return 'Processing';
      case 'SecurityCheck': return 'Security Check';
      case 'CancelByUser': return 'Cancelled';
      case 'Reject': return 'Rejected';
      case 'Fail': return 'Failed';
      case 'BlockchainConfirmed': return 'Confirmed';
      default: return status || '-';
    }
  }

  loadReconciliation(): void {
    this.loadingReconciliation = true;
    this.confirmReconcile = false;
    this.exchangeService.getReconciliation(this.accountId).subscribe({
      next: (res) => {
        this.reconciliation = res.data ?? null;
        this.loadingReconciliation = false;
      },
      error: () => this.loadingReconciliation = false,
    });
  }

  doReconcile(): void {
    this.reconciling = true;
    this.exchangeService.reconcileAccount(this.accountId).subscribe({
      next: (res) => {
        this.reconciling = false;
        this.confirmReconcile = false;
        this.loadReconciliation();
        this.loadTransactions();
      },
      error: () => this.reconciling = false,
    });
  }
}
