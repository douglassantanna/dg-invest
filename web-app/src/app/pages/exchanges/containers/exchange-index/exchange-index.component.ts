import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ExchangeAccountDto } from 'src/app/core/models/exchange-account';
import { ExchangeService } from 'src/app/core/services/exchange.service';

@Component({
  selector: 'app-exchange-index',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './exchange-index.component.html',
})
export class ExchangeIndexComponent implements OnInit {
  private exchangeService = inject(ExchangeService);

  accounts: ExchangeAccountDto[] = [];
  loading = true;
  loadError = '';

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.loading = true;
    this.loadError = '';
    this.exchangeService.getExchangeAccounts().subscribe({
      next: response => {
        this.loading = false;
        if (!response.isSuccess) {
          this.loadError = response.message || 'Could not load exchange accounts';
          return;
        }
        this.accounts = (response.data ?? []) as ExchangeAccountDto[];
      },
      error: error => {
        this.loading = false;
        this.loadError = typeof error.error?.message === 'string'
          ? error.error.message
          : 'Could not load exchange accounts';
      },
    });
  }

  accountLink(account: ExchangeAccountDto): string[] {
    return account.exchangeName.toLowerCase() === 'bybit'
      ? ['/exchanges/bybit', account.accountId.toString()]
      : ['/exchanges'];
  }

  statusClasses(status: string): string {
    switch (status.toLowerCase()) {
      case 'connected':
      case 'ok':
        return 'bg-green-50 text-green-700 dark:bg-green-900/30 dark:text-green-300';
      case 'error':
      case 'err':
        return 'bg-red-50 text-red-700 dark:bg-red-900/30 dark:text-red-300';
      case 'paused':
        return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300';
      default:
        return 'bg-yellow-50 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-300';
    }
  }

  formatDate(value: string | null): string {
    return value ? new Date(value).toLocaleString() : 'Never';
  }
}
