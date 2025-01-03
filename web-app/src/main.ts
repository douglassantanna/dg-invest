/// <reference types="@angular/localize" />

import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors } from '@angular/common/http';
import { enableProdMode, importProvidersFrom } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app/app.component';
import { environment } from './environments/environment.development';
import { routes } from './app/app.routes';
import { AuthorizationInterceptor } from './app/core/interceptors/auth.interceptor';
import { InvalidTokenInterceptor } from './app/core/interceptors/invalid-token.interceptor';
import { loadingInterceptor } from './app/core/interceptors/loading.interceptor';
import { HashLocationStrategy, LocationStrategy } from '@angular/common';

if (environment.production) {
  enableProdMode();
}

bootstrapApplication(AppComponent, {
  providers: [
    Location, { provide: LocationStrategy, useClass: HashLocationStrategy },
    provideHttpClient(
      withInterceptors(
        [
          AuthorizationInterceptor,
          InvalidTokenInterceptor,
          loadingInterceptor
        ],)
    ),
    provideRouter(routes),
    provideAnimations(),
  ],
}).catch((err) => console.error(err));
