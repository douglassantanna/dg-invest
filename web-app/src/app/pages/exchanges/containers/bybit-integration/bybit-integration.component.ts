import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { BybitConnectionGroupDto, BybitSubaccountRowDto } from 'src/app/core/models/bybit-connection-group';
import { BybitSubMemberDto } from 'src/app/core/models/bybit-sub-member';
import { Response } from 'src/app/core/models/response';
import { SyncStatusDto } from 'src/app/core/models/sync-status';
import { ExchangeService } from 'src/app/core/services/exchange.service';

export interface BybitAccountRow extends BybitSubaccountRowDto {
  lastSyncAt: string | null;
  errorCount: number;
  lastErrorMessage: string | null;
}

@Component({
  selector: 'app-bybit-integration',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './bybit-integration.component.html',
})
export class BybitIntegrationComponent implements OnInit {
  private exchangeService = inject(ExchangeService);

  accounts: BybitAccountRow[] = [];
  groups: BybitConnectionGroupDto[] = [];
  subMembers: BybitSubMemberDto[] = [];
  apiKey = '';
  apiSecret = '';
  loading = true;
  saving = false;
  discovering = false;
  disconnecting = false;
  loadError = '';
  toastMessage = '';

  ngOnInit(): void {
    this.load();
  }

  get accountCount(): number {
    return this.accounts.length;
  }

  get enabledCount(): number {
    return this.accounts.filter(account => account.isEnabled).length;
  }

  get hasAccounts(): boolean {
    return this.accounts.length > 0;
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    forkJoin({
      groups: this.exchangeService.getBybitConnectionGroups().pipe(catchError((error: HttpErrorResponse) => of(this.errorResponse(error)))),
      statuses: this.exchangeService.getSyncStatuses().pipe(catchError(() => of({ data: [], isSuccess: true, message: 'ok' } as Response<SyncStatusDto[]>))),
      subMembers: this.exchangeService.getBybitSubMembers().pipe(catchError(() => of({ data: [], isSuccess: true, message: 'ok' } as Response<BybitSubMemberDto[]>))),
    }).subscribe(({ groups, statuses, subMembers }) => {
      this.loading = false;
      this.groups = (groups.data ?? []) as BybitConnectionGroupDto[];
      this.subMembers = subMembers.data ?? [];
      if (!groups.isSuccess) {
        this.loadError = groups.message || 'Failed to load Bybit accounts';
        this.accounts = [];
        return;
      }
      const syncStatuses = (statuses.data ?? []) as SyncStatusDto[];
      const statusByAccountId = new Map<number, SyncStatusDto>(syncStatuses.map(status => [status.accountId, status]));
      this.accounts = this.groups.flatMap(group => group.subaccounts).map(account => ({
        ...account,
        lastSyncAt: statusByAccountId.get(account.accountId)?.lastSyncAt ?? null,
        errorCount: statusByAccountId.get(account.accountId)?.errorCount ?? 0,
        lastErrorMessage: statusByAccountId.get(account.accountId)?.lastErrorMessage ?? null,
      }));
    });
  }

  connect(): void {
    if (this.saving || this.discovering) return;
    this.saving = true;
    this.loadError = '';
    this.exchangeService.saveBybitIntegrationCredentials(this.apiKey, this.apiSecret)
      .pipe(finalize(() => this.clearCredentialForm()))
      .subscribe({
      next: response => {
        if (!response.isSuccess) {
          this.saving = false;
          this.loadError = response.message;
          return;
        }
        this.discover(true);
      },
      error: error => {
        this.saving = false;
        this.loadError = this.errorMessage(error, 'Could not save Bybit credentials');
      },
    });
  }

  discover(fromOnboarding = false): void {
    this.discovering = true;
    this.loadError = '';
    this.exchangeService.syncBybitAccounts().subscribe({
      next: response => {
        this.discovering = false;
        this.toast(response.message);
        if (response.isSuccess) this.load();
      },
      error: error => {
        this.discovering = false;
        const message = this.errorMessage(error, fromOnboarding ? 'Credentials saved, but account discovery failed' : 'Account discovery failed');
        if (fromOnboarding) this.loadError = message;
        else this.toast(message);
      },
      complete: () => {
        this.saving = false;
      },
    });
  }

  disconnect(): void {
    if (!window.confirm('Disconnect Bybit? Account history remains available, but discovery and synchronization stop.')) return;
    this.disconnecting = true;
    this.loadError = '';
    this.exchangeService.disconnectBybit().subscribe({
      next: response => {
        this.disconnecting = false;
        this.toast(response.message);
        if (response.isSuccess) this.load();
      },
      error: error => {
        this.disconnecting = false;
        this.toast(this.errorMessage(error, 'Could not disconnect Bybit'));
      },
    });
  }

  statusLabel(account: BybitAccountRow): string {
    if (account.status === 'ok') return 'Connected';
    if (account.status === 'err') return 'Needs attention';
    if (account.status === 'paused') return 'Paused';
    return 'Setup needed';
  }

  statusClasses(account: BybitAccountRow): string {
    if (account.status === 'ok') return 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-300';
    if (account.status === 'err') return 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300';
    if (account.status === 'paused') return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300';
    return 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300';
  }

  credentialLabel(account: BybitAccountRow): string {
    return account.hasApiKey && account.hasApiSecret ? 'Configured' : 'Missing';
  }

  formatDate(value: string | null): string {
    return value ? new Date(value).toLocaleString() : 'Never';
  }

  toast(message: string): void {
    this.toastMessage = message;
    setTimeout(() => this.toastMessage = '', 3500);
  }

  private clearCredentialForm(): void {
    this.apiKey = '';
    this.apiSecret = '';
  }

  private errorResponse(error: HttpErrorResponse): Response<BybitConnectionGroupDto[]> {
    return { data: [], isSuccess: false, message: this.errorMessage(error, 'Failed to load Bybit accounts') };
  }

  private errorMessage(error: HttpErrorResponse, fallback: string): string {
    return typeof error.error?.message === 'string' ? error.error.message : fallback;
  }
}
