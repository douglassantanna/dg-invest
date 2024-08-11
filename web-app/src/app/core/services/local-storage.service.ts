import { Injectable } from '@angular/core';
import { local_storage_token } from 'src/environments/environment.development';
import { AppConfig } from '../models/app-config';

@Injectable({
  providedIn: 'root'
})
export class LocalStorageService {

  private appConfig: AppConfig = {} as AppConfig;

  private set(key: string, value: any) {
    localStorage.setItem(key, value);
  }
  private get(key: string) {
    return localStorage.getItem(key);
  }
  private remove(key: string) {
    localStorage.removeItem(key);
  }

  setToken(token: string) {
    this.appConfig.jwtToken = token;
    this.set(local_storage_token, JSON.stringify(this.appConfig));
  }

  getToken(): string | null {
    const storedConfig = this.get(local_storage_token);
    this.appConfig = storedConfig ? JSON.parse(storedConfig) : this.setToken("");
    return this.appConfig.jwtToken;
  }

  removeToken() {
    const storedConfig = this.get(local_storage_token);
    this.appConfig = storedConfig ? JSON.parse(storedConfig) : null;
    if (this.appConfig.jwtToken) {
      this.appConfig.jwtToken = null;
      this.set(local_storage_token, JSON.stringify(this.appConfig));
      return;
    }
  }

  updateToken(token: string) {
    const storedConfig = this.get(local_storage_token);
    this.appConfig = storedConfig ? JSON.parse(storedConfig) : null;
    if (this.appConfig.jwtToken) {
      this.appConfig.jwtToken = token;
      this.appConfig.hideZeroBalance = this.appConfig.hideZeroBalance;
      this.appConfig.sortBy = this.appConfig.sortBy;
      this.appConfig.sortOrder = this.appConfig.sortOrder;
      this.set(local_storage_token, JSON.stringify(this.appConfig));
      return;
    }
  }

  setHideZeroBalance(hideZeroBalance: boolean) {
    this.appConfig.hideZeroBalance = hideZeroBalance;
    this.appConfig.jwtToken = this.appConfig.jwtToken;
    this.appConfig.sortBy = this.appConfig.sortBy;
    this.appConfig.sortOrder = this.appConfig.sortOrder;
    this.set(local_storage_token, JSON.stringify(this.appConfig));
  }

  setAssetListSortOrder(sortOrder: string) {
    this.appConfig.hideZeroBalance = this.appConfig.hideZeroBalance;
    this.appConfig.jwtToken = this.appConfig.jwtToken;
    this.appConfig.sortBy = this.appConfig.sortBy;
    this.appConfig.sortOrder = sortOrder == 'asc' ? 'asc' : 'desc';
    this.set(local_storage_token, JSON.stringify(this.appConfig));
  }

  setAssetListSortBy(sortBy: string) {
    this.appConfig.hideZeroBalance = this.appConfig.hideZeroBalance;
    this.appConfig.jwtToken = this.appConfig.jwtToken;
    this.appConfig.sortBy = sortBy;
    this.appConfig.sortOrder = this.appConfig.sortOrder;
    this.set(local_storage_token, JSON.stringify(this.appConfig));
  }

  getHideZeroBalance(): boolean {
    const storedConfig = this.get(local_storage_token);
    this.appConfig = storedConfig ? JSON.parse(storedConfig) : null;
    return this.appConfig.hideZeroBalance;
  }

  getAssetListSortBy(): string {
    const storedConfig = this.get(local_storage_token);
    this.appConfig = storedConfig ? JSON.parse(storedConfig) : null;
    return this.appConfig.sortBy ?? 'symbol';
  }

  getAssetListSortOrder(): string {
    const storedConfig = this.get(local_storage_token);
    this.appConfig = storedConfig ? JSON.parse(storedConfig) : null;
    return this.appConfig.sortOrder ?? 'asc';
  }
}
