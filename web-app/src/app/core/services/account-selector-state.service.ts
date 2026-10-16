import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, finalize, map, of, shareReplay, switchMap, tap } from 'rxjs';
import { AccountSelectorOption } from '../models/account-selector-option';
import { Response } from '../models/response';
import { AccountService, CreateAccountCommand, SelectAccountRequest } from './account.service';

@Injectable({ providedIn: 'root' })
export class AccountSelectorStateService {
  private readonly accountService = inject(AccountService);
  private hasLoaded = false;
  private loadingRequest: Observable<AccountSelectorOption[]> | null = null;

  readonly options = signal<AccountSelectorOption[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly selectedAccount = computed(() => this.options().find(option => option.isSelected) ?? null);

  load(): Observable<AccountSelectorOption[]> {
    if (this.hasLoaded)
      return of(this.options());

    if (this.loadingRequest)
      return this.loadingRequest;

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.loadingRequest = this.accountService.getAccountSelectorOptions().pipe(
      tap(options => {
        this.options.set(options);
        this.hasLoaded = true;
      }),
      tap({ error: () => this.errorMessage.set('Could not load accounts. Please try again.') }),
      finalize(() => {
        this.isLoading.set(false);
        this.loadingRequest = null;
      }),
      shareReplay(1),
    );

    return this.loadingRequest;
  }

  reload(): Observable<AccountSelectorOption[]> {
    this.hasLoaded = false;
    return this.load();
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
      switchMap(() => this.reload()),
      map(() => undefined),
    );
  }
}
