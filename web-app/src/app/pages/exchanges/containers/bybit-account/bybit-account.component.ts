import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { BybitConnectionGroupDto, BybitSubaccountRowDto } from 'src/app/core/models/bybit-connection-group';
import { BybitSubMemberDto } from 'src/app/core/models/bybit-sub-member';
import { ExchangeAccountDetailDto, ExchangeConnectionDto, ExchangeTransactionDto } from 'src/app/core/models/exchange-account';
import { Response } from 'src/app/core/models/response';
import { SyncLogEntry } from 'src/app/core/models/sync-log-entry';
import { ExchangeService } from 'src/app/core/services/exchange.service';

@Component({
  selector: 'app-bybit-account',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './bybit-account.component.html',
})
export class BybitAccountComponent implements OnInit {
  private exchangeService = inject(ExchangeService);
  private route = inject(ActivatedRoute);

  accountId = Number(this.route.snapshot.paramMap.get('accountId'));
  account: ExchangeAccountDetailDto | null = null;
  transactions: ExchangeTransactionDto[] = [];
  logs: SyncLogEntry[] = [];
  subMembers: BybitSubMemberDto[] = [];
  connectionRow: BybitSubaccountRowDto | null = null;
  apiKey = '';
  apiSecret = '';
  webhookSecret = '';
  selectedExternalId = '';
  loading = true;
  saving = false;
  testing = false;
  toggling = false;
  mapping = false;
  removing = false;
  loadError = '';
  toastMessage = '';

  ngOnInit(): void {
    this.load();
  }

  get connection(): ExchangeConnectionDto | null {
    return this.account?.connections[0] ?? null;
  }

  get name(): string {
    return this.account?.accountName ?? this.connectionRow?.name ?? 'Exchange account';
  }

  get canMap(): boolean {
    return !!this.selectedExternalId && !this.mapping;
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    forkJoin({
      detail: this.exchangeService.getExchangeAccountDetail(this.accountId).pipe(catchError((error: HttpErrorResponse) => of(this.failedResponse<ExchangeAccountDetailDto>(error, 'Could not load exchange account')))),
      transactions: this.exchangeService.getExchangeTransactions(this.accountId).pipe(catchError(() => of(this.emptyResponse<ExchangeTransactionDto[]>()))),
      logs: this.exchangeService.getSyncLogs(this.accountId).pipe(catchError(() => of(this.emptyResponse<SyncLogEntry[]>()))),
      groups: this.exchangeService.getBybitConnectionGroups().pipe(catchError(() => of(this.emptyResponse<BybitConnectionGroupDto[]>()))),
      subMembers: this.exchangeService.getBybitSubMembers().pipe(catchError(() => of(this.emptyResponse<BybitSubMemberDto[]>()))),
    }).subscribe(({ detail, transactions, logs, groups, subMembers }) => {
      this.loading = false;
      if (!detail.isSuccess || !detail.data) {
        this.loadError = detail.message;
        return;
      }
      this.account = detail.data;
      this.transactions = transactions.data ?? [];
      this.logs = logs.data ?? [];
      this.subMembers = subMembers.data ?? [];
      const connectionGroups = (groups.data ?? []) as BybitConnectionGroupDto[];
      this.connectionRow = connectionGroups.flatMap(group => group.subaccounts).find(account => account.accountId === this.accountId) ?? null;
      this.selectedExternalId = this.connectionRow?.externalId ?? '';
    });
  }

  saveCredentials(): void {
    if (this.saving) return;
    this.saving = true;
    this.loadError = '';
    this.exchangeService.saveBybitCredentials(this.accountId, this.apiKey, this.apiSecret, this.webhookSecret)
      .pipe(finalize(() => this.clearCredentialForm()))
      .subscribe({
        next: response => {
          this.saving = false;
          if (response.isSuccess) {
            this.toast(response.message);
            this.load();
          } else {
            this.loadError = response.message;
          }
        },
        error: error => {
          this.saving = false;
          this.loadError = this.errorMessage(error, 'Could not save credentials');
        },
      });
  }

  testConnection(): void {
    this.testing = true;
    this.exchangeService.testBybitConnection(this.accountId).subscribe({
      next: response => {
        this.testing = false;
        this.toast(response.message);
        if (response.isSuccess) this.load();
      },
      error: error => {
        this.testing = false;
        this.toast(this.errorMessage(error, 'Connection test failed'));
      },
    });
  }

  toggle(): void {
    this.toggling = true;
    this.exchangeService.toggleBybitAccount(this.accountId).subscribe({
      next: response => {
        this.toggling = false;
        this.toast(response.message);
        if (response.isSuccess) this.load();
      },
      error: error => {
        this.toggling = false;
        this.toast(this.errorMessage(error, 'Could not update synchronization'));
      },
    });
  }

  mapAccount(): void {
    if (!this.canMap) return;
    this.mapping = true;
    this.exchangeService.mapBybitAccount(this.accountId, this.selectedExternalId).subscribe({
      next: response => {
        this.mapping = false;
        this.toast(response.message);
        if (response.isSuccess) this.load();
      },
      error: error => {
        this.mapping = false;
        this.toast(this.errorMessage(error, 'Could not map account'));
      },
    });
  }

  removeAccount(): void {
    if (!window.confirm('Remove this Bybit exchange account? Its portfolio history remains available.')) return;
    this.removing = true;
    this.exchangeService.deleteCredentials(this.accountId).subscribe({
      next: response => {
        this.removing = false;
        this.toast(response.message);
        if (response.isSuccess) this.load();
      },
      error: error => {
        this.removing = false;
        this.toast(this.errorMessage(error, 'Could not remove the exchange account'));
      },
    });
  }

  statusLabel(): string {
    if (this.connectionRow?.status === 'ok' || this.connection?.status === 'Connected') return 'Connected';
    if (this.connectionRow?.status === 'err' || (this.connection?.errorCount ?? 0) > 0) return 'Error';
    if (this.connectionRow?.status === 'paused') return 'Synchronization paused';
    return 'Setup needed';
  }

  statusClasses(): string {
    const label = this.statusLabel();
    if (label === 'Connected') return 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-300';
    if (label === 'Error') return 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300';
    if (label === 'Synchronization paused') return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300';
    return 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300';
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
    this.webhookSecret = '';
  }

  private emptyResponse<T>(): Response<T> {
    return { data: [], isSuccess: true, message: 'ok' };
  }

  private failedResponse<T>(error: HttpErrorResponse, fallback: string): Response<T> {
    return { data: null, isSuccess: false, message: this.errorMessage(error, fallback) };
  }

  private errorMessage(error: HttpErrorResponse, fallback: string): string {
    return typeof error.error?.message === 'string' ? error.error.message : fallback;
  }
}
