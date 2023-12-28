import { Pagination } from '../models/pagination';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, of, switchMap, tap } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { AddTransactionCommand } from '../models/add-transaction-command';
import { CreateCryptoAssetCommand } from '../models/create-crypto-asset-command';
import { ViewCryptoAssetDto } from '../models/view-crypto-asset-dto';
import { ViewMinimalCryptoAssetDto } from '../models/view-minimal-crypto-asset-dto';
import { Response } from '../models/response';
import { CryptoAssetData } from '../models/crypto-asset-data';
import { CryptoInformation } from '../models/crypto-information';
import { CryptoTransactionHistory } from '../models/crypto-transaction-history';
import { ViewCryptoDataDto } from '../models/view-crypto-data-dto';
import { ToastService } from './toast.service';

const url = `${environment.apiUrl}/Crypto`;

@Injectable({
  providedIn: 'root'
})
export class CryptoService {

  private _cryptoAssetData: BehaviorSubject<CryptoAssetData[]> = new BehaviorSubject<CryptoAssetData[]>([]);
  private _cryptoInformation: BehaviorSubject<CryptoInformation[]> = new BehaviorSubject<CryptoInformation[]>([]);
  private _transactions: BehaviorSubject<CryptoTransactionHistory[]> = new BehaviorSubject<CryptoTransactionHistory[]>([]);

  constructor(private http: HttpClient, private toastService: ToastService) { }

  getCryptoAssets(
    page: number = 1,
    pageSize: number = 50,
    cryptoCurrency: string = "",
    sortOrder: string = "ASC",
    hideZeroBalance: boolean): Observable<Pagination<ViewMinimalCryptoAssetDto>> {
    let params = new HttpParams()
      .append("page", page)
      .append("pageSize", pageSize)
      .append("cryptoCurrency", cryptoCurrency)
      .append("sortOrder", sortOrder)
      .append("hideZeroBalance", hideZeroBalance);

    return this.http.get<Pagination<ViewMinimalCryptoAssetDto>>(`${url}/list-assets`, {
      params: params
    }).pipe(
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
  }

  getCryptos(): Observable<Response<Crypto>> {
    return this.http.get<Response<Crypto>>(`${url}/get-cryptos`).pipe(
      tap(),
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
  }

  getCryptoAssetById(id: number): Observable<Response<ViewCryptoAssetDto>> {
    return this.http.get<Response<ViewCryptoAssetDto>>(`${url}/get-crypto-asset-by-id/${id}`).pipe(
      tap((response: Response<Crypto>) => {
        this._cryptoAssetData.next(response.data.cryptoAssetData);
        this._cryptoInformation.next(response.data.cryptoInformation);
        this._transactions.next(response.data.transactions);
      }),
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
  }

  private getCryptoDataById(id: number): Observable<Response<ViewCryptoDataDto>> {
    return this.http.get<Response<ViewCryptoDataDto>>(`${url}/get-crypto-data-by-id/${id}`)
      .pipe(
        catchError(error => {
          if (error.error.message)
            this.toastService.showError(error.error.message);
          return of();
        })
      );
  }

  createCryptoAsset(command: CreateCryptoAssetCommand): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/create`, command).pipe(
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
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
          return this.getCryptoDataById(command.cryptoAssetId);
        } else {
          return of(this._cryptoAssetData.value);
        }
      }),
      tap((updatedCryptoData: CryptoAssetData[] | Response<ViewCryptoDataDto>) => {
        if (!Array.isArray(updatedCryptoData)) {
          this._cryptoAssetData.next(updatedCryptoData.data.cryptoAssetData);
        }
      }),
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
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
