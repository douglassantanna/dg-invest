import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExchangeService } from 'src/app/core/services/exchange.service';
import { AccountService } from 'src/app/core/services/account.service';
import { SyncStatusDto } from 'src/app/core/models/sync-status';
import { SyncLogEntry } from 'src/app/core/models/sync-log-entry';
import { BybitSubMemberDto } from 'src/app/core/models/bybit-sub-member';
import { ModalComponent } from 'src/app/layout/modal/modal.component';

@Component({
  selector: 'app-exchange-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './exchange-management.component.html',
})
export class ExchangeManagementComponent implements OnInit {
  private exchangeService = inject(ExchangeService);
  private accountService = inject(AccountService);

  accounts: { id: number; tag: string }[] = [];
  selectedAccountId = 0;

  // Credentials form
  apiKey = '';
  apiSecret = '';
  webhookSecret = '';
  savingCredentials = false;
  credentialsMessage = '';

  // Sync accounts
  syncing = false;
  syncMessage = '';

  // Sub-members
  subMembers: BybitSubMemberDto[] = [];
  loadingSubMembers = false;

  // Sync statuses
  syncStatuses: SyncStatusDto[] = [];
  loadingStatuses = false;

  // Sync logs
  selectedLogAccountId = 0;
  syncLogs: SyncLogEntry[] = [];
  loadingLogs = false;
  logDate = '';

  ngOnInit(): void {
    this.loadAccounts();
    this.loadSyncStatuses();
  }

  private loadAccounts(): void {
    this.accountService.getAccounts().subscribe({
      next: (result) => {
        this.accounts = result.map((a) => ({ id: a.id, tag: a.subaccountTag }));
        if (this.accounts.length > 0) this.selectedAccountId = this.accounts[0].id;
      },
    });
  }

  selectedAccountTag(): string {
    return this.accounts.find(a => a.id === this.selectedAccountId)?.tag ?? 'selected';
  }

  saveCredentials(): void {
    if (!this.selectedAccountId) return;
    this.savingCredentials = true;
    this.credentialsMessage = '';
    this.exchangeService
      .saveBybitCredentials(this.selectedAccountId, this.apiKey, this.apiSecret, this.webhookSecret)
      .subscribe({
        next: (res) => {
          this.credentialsMessage = res.message;
          if (res.isSuccess) {
            this.apiKey = '';
            this.apiSecret = '';
            this.webhookSecret = '';
          }
          this.savingCredentials = false;
        },
        error: () => {
          this.credentialsMessage = 'Failed to save credentials';
          this.savingCredentials = false;
        },
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
    this.exchangeService.mapBybitAccount(this.selectedAccountId, member.uid).subscribe({
      next: (res) => {
        if (res.isSuccess) this.loadSubMembers();
      },
    });
  }

  private loadSyncStatuses(): void {
    this.loadingStatuses = true;
    this.exchangeService.getSyncStatuses().subscribe({
      next: (res) => {
        this.syncStatuses = res.data ?? [];
        this.loadingStatuses = false;
      },
      error: () => this.loadingStatuses = false,
    });
  }

  toggleSyncLogs(accountId: number): void {
    if (this.selectedLogAccountId === accountId) {
      this.selectedLogAccountId = 0;
      return;
    }
    this.selectedLogAccountId = accountId;
    this.fetchSyncLogs(accountId);
  }

  onLogDateChange(accountId: number, date: string): void {
    this.logDate = date;
    this.fetchSyncLogs(accountId);
  }

  private fetchSyncLogs(accountId: number): void {
    this.loadingLogs = true;
    this.exchangeService.getSyncLogs(accountId, this.logDate || undefined).subscribe({
      next: (res) => {
        this.syncLogs = res.data ?? [];
        this.loadingLogs = false;
      },
      error: () => this.loadingLogs = false,
    });
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Connected': return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300';
      case 'Error': return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300';
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
}
