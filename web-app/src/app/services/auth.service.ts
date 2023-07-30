import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, distinctUntilChanged, Observable, of, tap } from 'rxjs';

import { LoginResponse } from '../interfaces/login-response';
import { User } from '../interfaces/user.model';
import { API_URL } from './api.token';

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

  constructor(
    @Inject(API_URL) private api: string,
    private http: HttpClient,
    private router: Router
  ) { }

  private auth = new BehaviorSubject<AuthState>(this.getLocalState());
  public auth$ = this.auth.asObservable().pipe(distinctUntilChanged());


  get state(): AuthState {
    return this.auth.getValue();
  }

  get role(): string | null {
    return this.state.role;
  }

  adminExists$ = of(true);

  profile$ = this.http.get<User>(`${this.api}/auth/user`);

  public getLocalState(): AuthState {
    const localState = localStorage.getItem('auth');
    if (localState) {
      return JSON.parse(localState) as AuthState;
    }
    return initialState;
  }

  login(credentials: Credential): Observable<LoginResponse> {
    const path = `${this.api}/login`;
    return this.http.post<LoginResponse>(path, credentials).pipe(
      tap(data => {
        this.auth.next(data);
        localStorage.setItem('auth', JSON.stringify(data));
        this.router.navigateByUrl('/dashboard').then();
      })
    );
  }

  changeName(name: string): Observable<User> {
    return this.http
      .post<User>(`${this.api}/auth/changeName`, { name })
      .pipe(tap(_ => this.auth.next({ ...this.auth.getValue(), name })));
  }

  changePassword(payload: any): Observable<User> {
    return this.http.post<User>(`${this.api}/auth/changePassword`, payload);
  }

  logout(): void {
    localStorage.removeItem('auth');
    this.auth.next(initialState);
    this.router.navigateByUrl('auth/login').then();
  }
}

