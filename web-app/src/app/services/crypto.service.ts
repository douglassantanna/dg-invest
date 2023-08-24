import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment.development';

const url = `${environment.apiUrl}/cryptos`;

export interface CryptoDto {
  id: number,
  currencyName: string,
  cryptoCurrency: string,
  symbol: string,
  createdAt: Date,
  transactions: Transaction[],
  balance: number,
  addresses: Address[],
  averagePrice: number
  totalSpent: number
}

export interface CreateTransactionCommand {
  amount: number,
  price: number,
  purchaseDate: Date,
  exchangeName: string,
  transactionType: ETransactionType,
  cryptoAssedId: number,
}

export interface Transaction {
  amount: number,
  price: number,
  purchaseDate: Date,
  exchangeName: string,
  transactionType: ETransactionType
}
export enum ETransactionType {
  Buy = 1,
  Sell = 2,
}
export interface Address {
  id: number,
  addressName: string,
  addressValue: string
}
export interface ViewCrypto {
  id: number,
  currencyName: string,
  cryptoCurrency: string,
  symbol: string,
  averagePrice: number,
  priceDifferencePercent: number
}

export interface Pagination<T> {
  items: T[],
  page: number,
  pageSize: number,
  totalCount: number
  hasNextPage: boolean,
  hasPreviousPage: boolean
}

export interface Response {
  message: string,
  success: boolean,
  data: any
}
@Injectable({
  providedIn: 'root'
})
export class CryptoService {

  constructor(private http: HttpClient) { }

  listCryptos(
    page: number = 0,
    pageSize: number = 10,
    currencyName: string = "",
    cryptoCurrency: string = "",
    sortColumn: string = "",
    sortOrder: string = "asc"
  ): Observable<Pagination<ViewCrypto>> {
    let params = new HttpParams()
      .append("page", page)
      .append("pageSize", pageSize)
      .append("currencyName", currencyName)
      .append("cryptoCurrency", cryptoCurrency)
      .append("sortColumn", sortColumn)
      .append("sortOrder", sortOrder);
    return this.http.get<Pagination<ViewCrypto>>(`${url}/list-assets`, {
      params: params
    });
  }

  getById(id: number): Observable<CryptoDto> {
    return this.http.get<CryptoDto>(`${url}/id/${id}`);
  }

  createTransaction(transaction: CreateTransactionCommand): Observable<Response> {
    return this.http.post<Response>(`${url}/create-crypto-transaction`, transaction);
  }
}
