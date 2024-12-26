import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { CustomRespose } from '../models/custom-response';
import { AccountDto } from './user.service';
import { DepositFundCommand, WithdrawFundCommand } from '../models/deposit-fund-command';
import { AddTransactionCommand } from '../models/add-transaction-command';
import { SimpleAccountDto } from 'src/app/pages/cryptos/components/account-selection/account-selection.component';

export interface CreateAccountCommand {
  subaccountTag: string;
}

export interface AddCryptoAssetRequest {
  symbol: string;
  coinMarketCapId: number;
  subAccountTag: string;
}

export interface SelectAccountRequest {
  accountId: number;
}

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private readonly apiUrl = `${environment.apiUrl}/Account`;
  private readonly http = inject(HttpClient);

  getAccounts(): Observable<SimpleAccountDto[]> {
    return this.http.get<SimpleAccountDto[]>(`${this.apiUrl}`);
  }

  createAccount(command: CreateAccountCommand): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(`${this.apiUrl}/create`, command);
  }

  getAccountBySubAccountTag(subAccountTag: string): Observable<AccountDto> {
    return this.http.get<AccountDto>(`${this.apiUrl}/${subAccountTag}`);
  }

  addCryptoAsset(request: AddCryptoAssetRequest): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(
      `${this.apiUrl}/add-crypto-asset`,
      request
    );
  }

  addTransaction(command: AddTransactionCommand): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(
      `${this.apiUrl}/add-transaction`,
      command
    );
  }

  depositFund(deposit: DepositFundCommand): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/deposit-fund`, deposit)
  }

  withdrawFund(subAccountTag: string, withdraw: WithdrawFundCommand): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${subAccountTag}/withdraw-fund`, withdraw)
  }

  selectAccount(command: SelectAccountRequest): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/select-account`, command)
  }
}
