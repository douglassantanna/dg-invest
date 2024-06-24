import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CreateUserCommand } from '../models/create-user';
import { environment } from 'src/environments/environment.development';
import { Observable, catchError, of } from 'rxjs';
import { ToastService } from './toast.service';
import { Response } from '../models/response';
import { Pagination } from '../models/pagination';
import { ViewUserDto } from '../models/view-user-dto';

const url = `${environment.apiUrl}/User`;

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(
    private http: HttpClient,
    private toastService: ToastService) { }

  createUser(command: CreateUserCommand) {
    return this.http.post<Response<any>>(`${url}/create`, command).pipe(
      catchError(error => {
        if (error.error.message)
          this.toastService.showError(error.error.message);
        return of();
      })
    );
  }

  updateUserProfile(fullname: string, email: string): Observable<Response<any>> {
    const command = { fullname, email };
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
}
