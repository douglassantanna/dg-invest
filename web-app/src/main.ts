import { provideHttpClient } from '@angular/common/http';
import { enableProdMode } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter, Routes } from '@angular/router';

import { AppComponent } from './app/app.component';
import { DashboardComponent } from './app/dashboard.component';
import { LoginComponent } from './app/login.component';
import { ViewCryptosComponent } from './app/view-cryptos.component';
import { environment } from './environments/environment.development';

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
    path: "login",
    component: LoginComponent,
  },
];

bootstrapApplication(AppComponent, {
  providers: [
    provideHttpClient(),
    provideRouter(routes),
    provideAnimations()
  ],
}).catch((err) => console.error(err));
