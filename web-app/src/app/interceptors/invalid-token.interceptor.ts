import { inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandlerFn
} from '@angular/common/http';
import { catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export function InvalidTokenInterceptor(req: HttpRequest<unknown>,
  next: HttpHandlerFn) {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((errorResponse) => {
      if (errorResponse.status === 401 && !req.url.includes('auth')) {
        authService.logout();
      }
      throw errorResponse;
    })
  );
}
