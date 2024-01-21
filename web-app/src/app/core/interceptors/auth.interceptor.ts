import { inject } from '@angular/core';
import {
  HttpRequest,
  HttpHandlerFn
} from '@angular/common/http';
import { LocalStorageService } from '../services/local-storage.service';

export function AuthorizationInterceptor(req: HttpRequest<unknown>,
  next: HttpHandlerFn) {
  const localStorageService = inject(LocalStorageService);
  const token = localStorageService.getToken();
  const clonedRequest = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });
  return next(clonedRequest)
}
