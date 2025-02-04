import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, delay, finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';


export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);
  loadingService.show();
  return next(req)
    .pipe(
      finalize(() => { loadingService.hide() }),
      catchError((error) => {
        loadingService.hide();
        throw error;
      })
    );
};
