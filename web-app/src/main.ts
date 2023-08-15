import { provideHttpClient } from '@angular/common/http';
import { enableProdMode, importProvidersFrom } from '@angular/core';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialogModule } from '@angular/material/dialog';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { Routes, provideRouter } from '@angular/router';

import { AppComponent } from './app/app.component';
import { environment } from './environments/environment.development';
import { CreateCryptoComponent } from './app/create-crypto.component';
import { CryptoDashboardComponent } from './app/crypto-dashboard.component';
import { DashboardComponent } from './app/dashboard.component';
import { LoginComponent } from './app/login.component';
import { ProfileComponent } from './app/profile.component';
import { ViewCryptosComponent } from './app/view-cryptos.component';

if (environment.production) {
  enableProdMode();
}

const routes: Routes = [
  {
    path: "",
    pathMatch: "full",
    redirectTo: "dashboard",
  },
  {
    path: "dashboard",
    component: DashboardComponent,
  },
  {
    path: "cryptos",
    component: ViewCryptosComponent,
  },
  {
    path: "create-crypto",
    component: CreateCryptoComponent,
  },
  {
    path: "crypto-dashboard/:cryptoId",
    component: CryptoDashboardComponent,
  },
  {
    path: "login",
    component: LoginComponent,
  },
  {
    path: "profile",
    component: ProfileComponent,
  },
];

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    provideRouter(routes),
    provideAnimations(),
    importProvidersFrom(MatDialogModule),
    importProvidersFrom(MatDatepickerModule),
    importProvidersFrom(MatNativeDateModule),
  ],
}).catch((err) => console.error(err));
