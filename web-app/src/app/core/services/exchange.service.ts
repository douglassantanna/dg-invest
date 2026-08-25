import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment.development';
import { Response } from '../models/response';
import { SyncStatusDto } from '../models/sync-status';
import { SyncLogEntry } from '../models/sync-log-entry';
import { BybitSubMemberDto } from '../models/bybit-sub-member';
import { CredentialsStatusDto } from '../models/credentials-status';
import { BybitConnectionGroupDto } from '../models/bybit-connection-group';
import { ExchangeAccountDetailDto, ExchangeTransactionDto } from '../models/exchange-account';

const url = `${environment.apiUrl}/Exchange`;

@Injectable({ providedIn: 'root' })
export class ExchangeService {
  private http = inject(HttpClient);

  saveBybitIntegrationCredentials(apiKey: string, apiSecret: string): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/integration-credentials`, { apiKey, apiSecret });
  }

  saveBybitCredentials(accountId: number, apiKey: string, apiSecret: string, webhookSecret: string, name?: string, externalId?: string): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/credentials`, { accountId, apiKey, apiSecret, webhookSecret, name, externalId });
  }

  getExchangeAccountDetail(accountId: number): Observable<Response<ExchangeAccountDetailDto>> {
    return this.http.get<Response<ExchangeAccountDetailDto>>(`${url}/${accountId}`);
  }

  getExchangeTransactions(accountId: number, limit = 20): Observable<Response<ExchangeTransactionDto[]>> {
    return this.http.get<Response<ExchangeTransactionDto[]>>(`${url}/${accountId}/transactions`, {
      params: new HttpParams().set('limit', limit),
    });
  }

  disconnectBybit(): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/disconnect`, {});
  }

  syncBybitAccounts(): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/sync-accounts`, {});
  }

  getBybitSubMembers(): Observable<Response<BybitSubMemberDto[]>> {
    return this.http.get<Response<BybitSubMemberDto[]>>(`${url}/bybit/sub-members`);
  }

  mapBybitAccount(accountId: number, externalId: string): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/map-account`, { accountId, externalId });
  }

  getCredentialsStatus(): Observable<Response<CredentialsStatusDto[]>> {
    return this.http.get<Response<CredentialsStatusDto[]>>(`${url}/bybit/credentials-status`);
  }

  deleteCredentials(accountId: number): Observable<Response<any>> {
    return this.http.delete<Response<any>>(`${url}/bybit/credentials/${accountId}`);
  }

  getSyncStatuses(): Observable<Response<SyncStatusDto[]>> {
    return this.http.get<Response<SyncStatusDto[]>>(`${url}/bybit/sync-status`);
  }

  getSyncLogs(accountId: number, date?: string): Observable<Response<SyncLogEntry[]>> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<Response<SyncLogEntry[]>>(`${url}/bybit/sync-logs/${accountId}`, { params });
  }

  getBybitConnectionGroups(): Observable<Response<BybitConnectionGroupDto[]>> {
    return this.http.get<Response<BybitConnectionGroupDto[]>>(`${url}/bybit/connection-groups`);
  }

  testBybitConnection(accountId: number): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/test-connection/${accountId}`, {});
  }

  toggleBybitAccount(accountId: number): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/toggle/${accountId}`, {});
  }
}
