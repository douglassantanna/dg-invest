import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, finalize, map, switchMap, tap } from 'rxjs';
import { AccountSelectorOption } from '../models/account-selector-option';
import { Response } from '../models/response';
import { AccountService, CreateAccountCommand, SelectAccountRequest } from './account.service';

@Injectable({ providedIn: 'root' })
export class AccountSelectorStateService {
  private readonly accountService = inject(AccountService);
  private hasLoaded = false;

  readonly options = signal<AccountSelectorOption[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly selectedAccount = computed(() => this.options().find(option => option.isSelected) ?? null);

  load(): void {
    if (this.hasLoaded || this.isLoading()) return;
    this.reload().subscribe({ error: () => undefined });
  }

  reload(): Observable<AccountSelectorOption[]> {
    this.isLoading.set(true);
    this.errorMessage.set('');
    return this.accountService.getAccountSelectorOptions().pipe(
      tap(options => {
        this.options.set(options);
        this.hasLoaded = true;
      }),
      tap({ error: () => this.errorMessage.set('Could not load accounts. Please try again.') }),
      finalize(() => this.isLoading.set(false)),
    );
  }

  selectAccount(accountId: number): Observable<Response<null>> {
    const request: SelectAccountRequest = { accountId };
    return this.accountService.selectAccount(request).pipe(tap(response => {
      if (response?.isSuccess === false) return;
      this.options.update(options => options.map(option => ({
        ...option,
        isSelected: option.accountId === accountId,
      })));
    }));
  }

  createManualPortfolio(command: CreateAccountCommand): Observable<void> {
    return this.accountService.createAccount(command).pipe(
      switchMap(() => this.accountService.getAccountSelectorOptions()),
      tap(options => {
        this.options.set(options);
        this.hasLoaded = true;
      }),
      map(() => undefined),
    );
  }
}
