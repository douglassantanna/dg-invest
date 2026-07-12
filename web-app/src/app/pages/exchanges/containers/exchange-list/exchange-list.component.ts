import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ExchangeService } from 'src/app/core/services/exchange.service';
import { ExchangeAccountDto } from 'src/app/core/models/exchange-account';
import { BybitSubMemberDto } from 'src/app/core/models/bybit-sub-member';

@Component({
  selector: 'app-exchange-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './exchange-list.component.html',
})
export class ExchangeListComponent implements OnInit {
  private exchangeService = inject(ExchangeService);

  accounts: ExchangeAccountDto[] = [];
  loading = true;

  // Sub-accounts
  subMembers: BybitSubMemberDto[] = [];
  loadingSubMembers = false;
  syncing = false;
  syncMessage = '';

  ngOnInit(): void {
    this.loadAccounts();
  }

  private loadAccounts(): void {
    this.loading = true;
    this.exchangeService.getExchangeAccounts().subscribe({
      next: (res) => {
        this.accounts = res.data ?? [];
        this.loading = false;
      },
      error: () => this.loading = false,
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
        this.loadAccounts();
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

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Connected': return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300';
      case 'Error': return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300';
      case 'Disconnected': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300';
    }
  }
}
