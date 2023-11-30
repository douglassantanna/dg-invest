import { Pagination } from '../models/pagination';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { AddTransactionCommand } from '../models/add-transaction-command';
import { CreateCryptoAssetCommand } from '../models/create-crypto-asset-command';
import { ViewCryptoAssetDto } from '../models/view-crypto-asset-dto';
import { ViewMinimalCryptoAssetDto } from '../models/view-minimal-crypto-asset-dto';
import { Response } from '../models/response';

const url = `${environment.apiUrl}/Crypto`;

@Injectable({
  providedIn: 'root'
})
export class CryptoService {

  constructor(private http: HttpClient) { }

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

  getCryptoAssetById(id: number): Observable<Response<ViewCryptoAssetDto>> {
    return this.http.get<Response<ViewCryptoAssetDto>>(`${url}/get-crypto-asset-by-id/${id}`)
  }

  createCryptoAsset(command: CreateCryptoAssetCommand): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/create`, command)
  }

  addTransaction(command: AddTransactionCommand) {
    return this.http.post<Response<any>>(`${url}/add-transaction`, command)
  }
}
