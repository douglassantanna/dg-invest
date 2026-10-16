import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, finalize, map, of, shareReplay, switchMap, tap } from 'rxjs';
import { AccountSelectorOption } from '../models/account-selector-option';
import { Response } from '../models/response';
import { AccountService, CreateAccountCommand, SelectAccountRequest } from './account.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class AccountSelectorStateService {
  private readonly accountService = inject(AccountService);
  private readonly authService = inject(AuthService);
  private hasLoaded = false;
  private currentUserId: string | null = null;
  private loadingRequestUserId: string | null = null;
  private loadingRequest: Observable<AccountSelectorOption[]> | null = null;

  readonly options = signal<AccountSelectorOption[]>([]);
  readonly isLoading = signal(false);
  readonly errorMessage = signal('');
  readonly selectedAccount = computed(() => this.options().find(option => option.isSelected) ?? null);

  load(): Observable<AccountSelectorOption[]> {
    const userId = this.authService.userId;
    if (this.currentUserId !== userId) {
      this.currentUserId = userId;
      this.hasLoaded = false;
      this.loadingRequest = null;
      this.loadingRequestUserId = null;
      this.options.set([]);
      this.errorMessage.set('');
      this.isLoading.set(false);
    }

    if (this.hasLoaded)
      return of(this.options());

    if (this.loadingRequest && this.loadingRequestUserId === userId)
      return this.loadingRequest;

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.loadingRequestUserId = userId;
    this.loadingRequest = this.accountService.getAccountSelectorOptions().pipe(
      tap(options => {
        if (this.currentUserId === userId) {
          this.options.set(options);
          this.hasLoaded = true;
        }
      }),
      tap({ error: () => {
        if (this.currentUserId === userId)
          this.errorMessage.set('Could not load accounts. Please try again.');
      } }),
      finalize(() => {
        if (this.loadingRequestUserId === userId) {
          this.isLoading.set(false);
          this.loadingRequest = null;
          this.loadingRequestUserId = null;
        }
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
    const userId = this.authService.userId;
    const request: SelectAccountRequest = { accountId };
    return this.accountService.selectAccount(request).pipe(tap(response => {
      if (this.currentUserId !== userId || response?.isSuccess === false) return;
      this.options.update(options => options.map(option => ({
        ...option,
        isSelected: option.accountId === accountId,
      })));
    }));
  }

  createManualPortfolio(command: CreateAccountCommand): Observable<void> {
    const userId = this.authService.userId;
    return this.accountService.createAccount(command).pipe(
      switchMap(() => this.currentUserId === userId ? this.reload() : of([])),
      map(() => undefined),
    );
  }
}
