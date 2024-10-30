/// <reference types="@angular/localize" />

import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptors } from '@angular/common/http';
import { enableProdMode, importProvidersFrom } from '@angular/core';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule } from '@angular/material/dialog';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app/app.component';
import { environment } from './environments/environment.development';
import { routes } from './app/app.routes';
import { AuthorizationInterceptor } from './app/core/interceptors/auth.interceptor';
import { InvalidTokenInterceptor } from './app/core/interceptors/invalid-token.interceptor';
import { loadingInterceptor } from './app/core/interceptors/loading.interceptor';
import { HashLocationStrategy, LocationStrategy, registerLocaleData } from '@angular/common';
import { provideNzIcons } from './icons-provider';
import { pt_BR, provideNzI18n } from 'ng-zorro-antd/i18n';
import pt from '@angular/common/locales/pt';
import { FormsModule } from '@angular/forms';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

registerLocaleData(pt);

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
    importProvidersFrom(MatDialogModule),
    importProvidersFrom(MatDatepickerModule),
    importProvidersFrom(MatNativeDateModule), provideNzIcons(), provideNzI18n(pt_BR), importProvidersFrom(FormsModule), provideAnimationsAsync(), provideHttpClient(),
  ],
}).catch((err) => console.error(err));
