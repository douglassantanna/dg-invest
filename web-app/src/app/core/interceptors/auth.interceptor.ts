import { inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandlerFn
} from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

export function AuthorizationInterceptor(req: HttpRequest<unknown>,
  next: HttpHandlerFn) {
  const authService = inject(AuthService);
  const clonedRequest = req.clone({
    setHeaders: {
      Authorization: `Bearer ${authService.token}`
    }
  });
  return next(clonedRequest)
}
