import { Pagination } from './../models/pagination';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { CreateCryptoAssetCommand } from '../portfolio/create-crypto.component';
import { ToastService } from './toast.service';

const url = `${environment.apiUrl}/Crypto`;

export interface ViewMinimalCryptoAssetDto {
  id: number;
  currencyName: string;
  cryptoCurrency: string;
  symbol: string;
  currentPrice: number;
  percentChange24h: number;
}

export interface Crypto {
  id: number;
  name: string;
  symbol: string;
  image: string;
  coinMarketCapId: number;
}

export interface Response<T> {
  data: T[];
  isSuccess: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class CryptoService {

  constructor(private http: HttpClient, private toastService: ToastService) { }

  getCryptoAssets(
    page: number = 1,
    pageSize: number = 10,
    cryptoCurrency: string = "",
    sortOrder: string = "ASC"): Observable<Pagination<ViewMinimalCryptoAssetDto>> {
    let params = new HttpParams()
      .append("page", page)
      .append("pageSize", pageSize)
      .append("cryptoCurrency", cryptoCurrency)
      .append("sortOrder", sortOrder)

    return this.http.get<Pagination<ViewMinimalCryptoAssetDto>>(`${url}/list-assets`, {
      params: params
    });
  }

  getCryptos(): Observable<Response<Crypto>> {
    return this.http.get<Response<Crypto>>(`${url}/get-cryptos`).pipe(
      tap()
    );
  }

  createCryptoAsset(command: CreateCryptoAssetCommand): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/create`, command)
  }
}
