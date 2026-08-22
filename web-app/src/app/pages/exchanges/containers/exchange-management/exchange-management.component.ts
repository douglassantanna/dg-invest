import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ExchangeService } from 'src/app/core/services/exchange.service';
import { BybitConnectionGroupDto, BybitSubaccountRowDto } from 'src/app/core/models/bybit-connection-group';
import { SyncStatusDto } from 'src/app/core/models/sync-status';

interface ExchangeAccount extends BybitSubaccountRowDto {
  lastSyncAt: string | null;
}

@Component({
  selector: 'app-exchange-management',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './exchange-management.component.html',
})
export class ExchangeManagementComponent implements OnInit {
  private exchangeService = inject(ExchangeService);

  accounts: ExchangeAccount[] = [];
  loading = true;
  syncingAccounts = false;
  testingAccountId: number | null = null;
  togglingAccountId: number | null = null;
  toastMessage = '';
  showToast = false;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadAccounts();
  }

  get configuredAccountCount(): number {
    return this.accounts.filter((account) => account.hasApiKey && account.hasApiSecret).length;
  }

  get enabledAccountCount(): number {
    return this.accounts.filter((account) => account.isEnabled).length;
  }

  statusMeta(status: string): { label: string; classes: string } {
    switch (status) {
      case 'ok':
        return { label: 'Connected', classes: 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-400' };
      case 'err':
        return { label: 'Error', classes: 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-400' };
      case 'paused':
        return { label: 'Sync disabled', classes: 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300' };
      default:
        return { label: 'Needs setup', classes: 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400' };
    }
  }

  loadAccounts(): void {
    this.loading = true;
    forkJoin({
      groups: this.exchangeService.getBybitConnectionGroups(),
      statuses: this.exchangeService.getSyncStatuses(),
    }).subscribe({
      next: ({ groups, statuses }) => {
        const statusByAccountId = new Map<number, SyncStatusDto>(
          (statuses.data ?? []).map((status: SyncStatusDto) => [status.accountId, status])
        );
        const connectionGroups = (groups.data ?? []) as BybitConnectionGroupDto[];
        this.accounts = connectionGroups.flatMap((group) => group.subaccounts).map((account) => ({
          ...account,
          lastSyncAt: statusByAccountId.get(account.accountId)?.lastSyncAt ?? null,
        }));
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.flashToast('Failed to load Bybit accounts');
      },
    });
  }

  syncAccounts(): void {
    this.syncingAccounts = true;
    this.exchangeService.syncBybitAccounts().subscribe({
      next: (response) => {
        this.syncingAccounts = false;
        this.flashToast(response.message);
        if (response.isSuccess) this.loadAccounts();
      },
      error: (error: HttpErrorResponse) => {
        this.syncingAccounts = false;
        this.flashToast(this.getErrorMessage(error, 'Account discovery failed'));
      },
    });
  }

  testConnection(account: ExchangeAccount): void {
    this.testingAccountId = account.accountId;
    this.exchangeService.testBybitConnection(account.accountId).subscribe({
      next: (response) => {
        this.testingAccountId = null;
        this.flashToast(response.message);
        if (response.isSuccess) this.loadAccounts();
      },
      error: (error: HttpErrorResponse) => {
        this.testingAccountId = null;
        this.flashToast(this.getErrorMessage(error, `Failed to test ${account.name}`));
      },
    });
  }

  toggleAccount(account: ExchangeAccount): void {
    this.togglingAccountId = account.accountId;
    this.exchangeService.toggleBybitAccount(account.accountId).subscribe({
      next: (response) => {
        this.togglingAccountId = null;
        this.flashToast(response.message);
        if (response.isSuccess) this.loadAccounts();
      },
      error: (error: HttpErrorResponse) => {
        this.togglingAccountId = null;
        this.flashToast(this.getErrorMessage(error, `Failed to update ${account.name}`));
      },
    });
  }

  formatDate(value: string | null): string {
    return value ? new Date(value).toLocaleString() : 'Never';
  }

  private getErrorMessage(error: HttpErrorResponse, fallback: string): string {
    return typeof error.error?.message === 'string' ? error.error.message : fallback;
  }

  flashToast(message: string): void {
    this.toastMessage = message;
    this.showToast = true;
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => this.showToast = false, 2500);
  }
}
