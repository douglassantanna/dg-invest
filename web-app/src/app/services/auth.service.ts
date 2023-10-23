import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import jwt_decode from 'jwt-decode';
import { environment } from 'src/environments/environment.development';
import { LoginFormModel } from '../models/login.form-models';
import { local_storage_token } from 'src/environments/environment.development';

const url = `${environment.apiUrl}/Authentication`;

export interface IUserDecode {
  fullName: string;
  email: string;
  role: string;
  nameid: string;
}

export interface LoginCommand {
  email: string;
  password: string;
}

export interface CustomRespose {
  message: string;
  isSuccess: boolean;
  data: any;
}

export interface AuthState {
  userId: number | null;
  access_token: string | null;
  token_type: string | null;
  expires_at: string | null;
  name: string | null;
  role: string | null;
}

export const initialState: AuthState = {
  userId: null,
  access_token: null,
  token_type: null,
  expires_at: null,
  name: null,
  role: null,
};

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  isLoggedIn$ = new BehaviorSubject<boolean>(false);
  isLoggedIn = this.isLoggedIn$.asObservable();
  private _user = new BehaviorSubject<IUserDecode>(
    this.decodePayloadJWT()
  );
  user = this._user.asObservable();
  constructor(
    private http: HttpClient,
    private router: Router
  ) { }

  get role(): string | null {
    return this.decodePayloadJWT() ? this.decodePayloadJWT().role : null;
  }

  get token(): any {
    return localStorage.getItem(local_storage_token);
  }

  setToken(token: any) {
    localStorage.setItem(local_storage_token, token as string);
    this._user.next(this.decodePayloadJWT());
    this.isLoggedIn$.next(true);
  }

  login(credentials: LoginFormModel): Observable<CustomRespose> {
    return this.http.post<CustomRespose>(`${url}/login`, credentials).pipe(
      tap((response: CustomRespose) => {
        this.setToken(response.data.token);
      })
    );
  }

  private decodePayloadJWT(): any {
    try {
      let response = jwt_decode(local_storage_token as string);
      return response;
    } catch (error) {
      return null;
    }
  }

  logout(): void {
    localStorage.removeItem(local_storage_token);
    this.isLoggedIn$.next(false);
    this.router.navigateByUrl('login');
  }
}

