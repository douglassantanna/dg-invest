import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import jwt_decode from 'jwt-decode';
import { environment } from 'src/environments/environment.development';
import { LoginFormModel } from '../models/login.form-models';
import { local_storage_token } from 'src/environments/environment.development';
import { CustomRespose } from '../models/custom-response';
import { UserDecode } from '../models/user-decode';
import { LocalStorageService } from './local-storage.service';

const url = `${environment.apiUrl}/Authentication`;

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  isLoggedIn$ = new BehaviorSubject<boolean>(false);
  isLoggedIn = this.isLoggedIn$.asObservable();
  private _user = new BehaviorSubject<UserDecode>(
    this.decodePayloadJWT()
  );

  user = this._user.asObservable();
  constructor(
    private http: HttpClient,
    private router: Router,
    private localStorageService: LocalStorageService
  ) {
    if (this.token)
      this.isLoggedIn$.next(true);
  }

  // get role(): string | null {
  //   return this.decodePayloadJWT() ? this.decodePayloadJWT().role : null;
  // }

  get userId(): string | null {
    try {
      const token = this.localStorageService.getToken();
      const decodedToken = jwt_decode(token as string) as { nameid: string };

      if (decodedToken) {
        return decodedToken.nameid;
      }

    } catch (error) {
      console.error('Error decoding token:', error);
    }

    return null;
  }



  get token(): string | null {
    return this.localStorageService.getToken();
  }

  private setToken(token: any) {
    this.localStorageService.setToken(token as string);
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
    this.localStorageService.removeToken();
    this.isLoggedIn$.next(false);
    this.router.navigateByUrl('login');
  }
}

