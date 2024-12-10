import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CreateUserCommand, Role } from '../models/create-user';
import { environment } from 'src/environments/environment.development';
import { Observable, catchError, of } from 'rxjs';
import { ToastService } from './toast.service';
import { Response } from '../models/response';
import { Pagination } from '../models/pagination';
import { ViewUserDto } from '../models/view-user-dto';
import { AccountTransactionType } from '../models/deposit-fund-command';

const url = `${environment.apiUrl}/User`;
export interface AccountTransactionDto {
  date: Date | string;
  transactionType: AccountTransactionType;
  amount: number;
  exchangeName: string;
  currency: string;
  destination: string;
  notes: string;
  cryptoCurrentPrice: number;
  cryptoSymbol: string;
  fee: number;
}

export interface GroupedAccountTransactionsDto {
  date: Date | string;
  transactions: AccountTransactionDto[];
}

export interface AccountDto {
  id: number;
  balance: number;
  groupedAccountTransactions: GroupedAccountTransactionsDto[];
}

export interface UserDto {
  id: number;
  fullName: string;
  email: string;
  role: Role;
  account?: AccountDto;
}
@Injectable({
  providedIn: 'root'
})
export class UserService {
  constructor(
    private http: HttpClient,
    private toastService: ToastService) { }

  updateUserPassword(arg: { userId: number; currentPassword: string; newPassword: string; confirmNewPassword: string; }): Observable<Response<any>> {
    return this.http.post<Response<any>>(`${url}/update-user-password`, arg);
  }

  createUser(command: CreateUserCommand) {
    return this.http.post<Response<any>>(`${url}/create`, command).pipe(
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
  }

  updateUserProfile(fullname: string, email: string, userId: string): Observable<Response<any>> {
    const command = { fullname, email, userId };
    return this.http.post<Response<any>>(`${url}/update-user-profile`, command);
  }

  getUsers(
    page: number = 1,
    pageSize: number = 50,
    fullName: string = "",
    sortOrder: string = "ASC"): Observable<Pagination<ViewUserDto>> {
    let params = new HttpParams()
      .append("page", page)
      .append("pageSize", pageSize)
      .append("fullName", fullName)
      .append("sortOrder", sortOrder);

    return this.http.get<Pagination<ViewUserDto>>(`${url}/list-users`, {
      params: params
    }).pipe(
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
  }

  getUserById(): Observable<any> {
    return this.http.get<any>(`${url}/get-user-by-id`);
  }
}
