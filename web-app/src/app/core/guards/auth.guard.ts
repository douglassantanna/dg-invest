import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { LocalStorageService } from '../services/local-storage.service';

export const authGuard: CanActivateFn = (): Observable<boolean> => {
  const router = inject(Router);
  const localStorageService = inject(LocalStorageService);
  const token = localStorageService.getToken();
  if (!token) {
    router.navigate(['/login']);
    return of(false);
  }
  return of(true);
};
