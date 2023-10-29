import { Pagination } from './../models/pagination';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment.development';

const url = `${environment.apiUrl}/Crypto`;

export interface ViewMinimalCryptoAssetDto {
  id: number;
  currencyName: string;
  cryptoCurrency: string;
  symbol: string;
  currentPrice: number;
}

@Injectable({
  providedIn: 'root'
})
export class CryptoService {

  constructor(private http: HttpClient) { }

  getCryptoAssets(
    page: number = 1,
    pageSize: number = 5,
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

}
