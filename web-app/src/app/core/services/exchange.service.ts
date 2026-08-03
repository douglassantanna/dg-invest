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

const url = `${environment.apiUrl}/Exchange`;

@Injectable({ providedIn: 'root' })
export class ExchangeService {
  private http = inject(HttpClient);

  saveBybitCredentials(accountId: number, apiKey: string, apiSecret: string, webhookSecret: string, subaccountTag?: string, bybitUid?: string): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/credentials`, { accountId, apiKey, apiSecret, webhookSecret, subaccountTag, bybitUid });
  }

  syncBybitAccounts(): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/sync-accounts`, {});
  }

  getBybitSubMembers(): Observable<Response<any>> {
    return this.http.get<Response<any>>(`${url}/bybit/sub-members`);
  }

  mapBybitAccount(accountId: number, bybitUid: string): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/bybit/map-account`, { accountId, bybitUid });
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
