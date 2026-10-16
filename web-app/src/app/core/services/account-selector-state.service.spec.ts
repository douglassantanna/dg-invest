import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { AccountSelectorOption } from '../models/account-selector-option';
import { AccountService } from './account.service';
import { AccountSelectorStateService } from './account-selector-state.service';
import { AuthService } from './auth.service';

describe('AccountSelectorStateService', () => {
  let currentUserId: string;
  let accountService: jasmine.SpyObj<AccountService>;
  let service: AccountSelectorStateService;

  const optionsFor = (accountId: number, accountName: string): AccountSelectorOption[] => [{
    accountId,
    accountName,
    balance: 100,
    isSelected: true,
    accountType: 'Manual',
    exchangeName: null,
    isBybitCombinedWallet: false,
    healthStatus: null,
    lastSyncAt: null,
    errorCount: 0,
    lastErrorMessage: null,
  }];

  beforeEach(() => {
    currentUserId = '1';
    accountService = jasmine.createSpyObj<AccountService>('AccountService', ['getAccountSelectorOptions']);
    accountService.getAccountSelectorOptions.and.callFake(() => of(
      currentUserId === '1'
        ? optionsFor(1, 'First user portfolio')
        : optionsFor(2, 'Second user portfolio')
    ));

    TestBed.configureTestingModule({
      providers: [
        AccountSelectorStateService,
        { provide: AccountService, useValue: accountService },
        { provide: AuthService, useValue: { get userId() { return currentUserId; } } },
      ],
    });
    service = TestBed.inject(AccountSelectorStateService);
  });

  it('reloads options when the authenticated user changes', () => {
    service.load().subscribe();
    expect(service.selectedAccount()?.accountName).toBe('First user portfolio');

    currentUserId = '2';
    service.load().subscribe();

    expect(accountService.getAccountSelectorOptions).toHaveBeenCalledTimes(2);
    expect(service.selectedAccount()?.accountName).toBe('Second user portfolio');
  });

  it('does not let an earlier user request overwrite the current user options', () => {
    const firstUserResponse = new Subject<AccountSelectorOption[]>();
    const secondUserResponse = new Subject<AccountSelectorOption[]>();
    accountService.getAccountSelectorOptions.and.returnValues(firstUserResponse, secondUserResponse);

    service.load().subscribe();
    currentUserId = '2';
    service.load().subscribe();

    secondUserResponse.next(optionsFor(2, 'Second user portfolio'));
    firstUserResponse.next(optionsFor(1, 'First user portfolio'));

    expect(service.selectedAccount()?.accountName).toBe('Second user portfolio');
  });
});
