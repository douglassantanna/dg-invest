import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (): Observable<boolean> => {
  const authService = inject(AuthService);
  authService.isLoggedIn.subscribe((x) => {
    console.log(x);

  })
  return authService.isLoggedIn;
};
