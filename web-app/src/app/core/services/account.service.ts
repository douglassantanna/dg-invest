import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { CustomRespose } from '../models/custom-response';
import { AccountDto } from './user.service';

export interface CreateAccountCommand {
  subaccountTag: string;
}

export interface AddCryptoAssetRequest {
  cryptoId: number;
}

export interface AddTransactionCommand {
  amount: number;
  price: number;
  purchaseDate: Date;
  exchangeName: string;
  transactionType: string;
  cryptoAssetId: number;
  fee: number;
}

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private readonly apiUrl = `${environment.apiUrl}/Account`;
  private readonly http = inject(HttpClient);

  getAccounts(): Observable<CustomRespose> {
    return this.http.get<CustomRespose>(`${this.apiUrl}`);
  }

  createAccount(command: CreateAccountCommand): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(`${this.apiUrl}/create`, command);
  }

  getAccountBySubAccountTag(subAccountTag: string): Observable<AccountDto> {
    return this.http.get<AccountDto>(`${this.apiUrl}/${subAccountTag}`);
  }

  addCryptoAsset(subAccountTag: string, request: AddCryptoAssetRequest): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(
      `${this.apiUrl}/${subAccountTag}/add-crypto-asset`,
      request
    );
  }

  addTransaction(subAccountTag: string, command: AddTransactionCommand): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(
      `${this.apiUrl}/${subAccountTag}/add-transaction`,
      command
    );
  }

}
