import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CreateUserCommand } from '../models/create-user';
import { environment } from 'src/environments/environment.development';
import { catchError, of } from 'rxjs';
import { ToastService } from './toast.service';
import { Response } from '../models/response';

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
}
