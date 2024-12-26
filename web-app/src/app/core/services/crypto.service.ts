import { Pagination } from '../models/pagination';
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { AddTransactionCommand } from '../models/add-transaction-command';
import { CreateCryptoAssetCommand } from '../models/create-crypto-asset-command';
import { ViewCryptoAssetDto } from '../models/view-crypto-asset-dto';
import { Crypto } from '../models/crypto';
import { Response } from '../models/response';
import { CryptoAssetData } from '../models/crypto-asset-data';
import { CryptoInformation } from '../models/crypto-information';
import { CryptoTransactionHistory } from '../models/crypto-transaction-history';
import { ViewCryptoDataDto } from '../models/view-crypto-data-dto';
import { UserCryptoAssetDto } from '../models/view-crypto-information';
import { AuthService } from './auth.service';

const url = `${environment.apiUrl}/Crypto`;

@Injectable({
  providedIn: 'root'
})
export class CryptoService {
  private _cryptoAssetData: BehaviorSubject<CryptoAssetData[]> = new BehaviorSubject<CryptoAssetData[]>([]);
  private _cryptoInformation: BehaviorSubject<CryptoInformation[]> = new BehaviorSubject<CryptoInformation[]>([]);
  private _transactions: BehaviorSubject<CryptoTransactionHistory[]> = new BehaviorSubject<CryptoTransactionHistory[]>([]);
  private _userId: BehaviorSubject<number> = new BehaviorSubject<number>(0);
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  getCryptoAssets(
    page: number = 1,
    pageSize: number = 50,
    assetName: string = "",
    sortBy: string = "ASC",
    sortOrder: string = "asc",
    hideZeroBalance: boolean = false): Observable<Pagination<UserCryptoAssetDto>> {
    let userId = this.authService.userId ?? '';
    let params = new HttpParams()
      .append("page", page)
      .append("pageSize", pageSize)
      .append("assetName", assetName)
      .append("sortBy", sortBy)
      .append("sortOrder", sortOrder)
      .append("hideZeroBalance", hideZeroBalance)
      .append("userId", userId)

    return this.http.get<Pagination<UserCryptoAssetDto>>(`${url}/list-assets`, {
      params: params
    });
  }

  getCryptos(): Observable<Response<Crypto>> {
    return this.http.get<Response<Crypto>>(`${url}/get-cryptos`);
  }

  getCryptoAssetById(id: number): Observable<Response<ViewCryptoAssetDto>> {
    return this.http.get<Response<ViewCryptoAssetDto>>(`${url}/get-crypto-asset-by-id/${id}`).pipe(
      tap((response: Response<Crypto>) => {
        this._cryptoAssetData.next(response.data.cryptoAssetData);
        this._cryptoInformation.next(response.data.cryptoInformation);
        this._transactions.next(response.data.transactions);
      })
    );
  }

  createCryptoAsset(command: CreateCryptoAssetCommand): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/create`, command);
  }

  addTransaction(command: AddTransactionCommand) {
    return this.http.post<Response<any>>(`${url}/add-transaction`, command).pipe(
      tap((response: Response<any>) => {
        if (response.isSuccess) {
          this._transactions.next([...this._transactions.value, this.convertTransactionCommandToDto(command)]);
        }
      }),
      switchMap((response: Response<any>) => {
        if (response.isSuccess) {
          return this.getCryptoAssetById(command.cryptoAssetId);
        } else {
          return of(this._cryptoAssetData.value);
        }
      }),
      tap((updatedCryptoData: CryptoAssetData[] | Response<ViewCryptoDataDto>) => {
        if (!Array.isArray(updatedCryptoData)) {
          this._cryptoAssetData.next(updatedCryptoData.data.cryptoAssetData);
        }
      }));
  }



  private convertTransactionCommandToDto(command: AddTransactionCommand): CryptoTransactionHistory {
    const transaction = {
      amount: command.amount,
      price: command.price,
      purchaseDate: command.purchaseDate,
      exchangeName: command.exchangeName,
      transactionType: command.transactionType,
    } as CryptoTransactionHistory;

    return transaction;
  }

  get transactions$(): Observable<CryptoTransactionHistory[]> {
    return this._transactions.asObservable();
  }

  get cryptoAssetData$(): Observable<CryptoAssetData[]> {
    return this._cryptoAssetData.asObservable();
  }
}
