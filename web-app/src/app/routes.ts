import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, Routes } from '@angular/router';

import { AppComponent } from './app.component';
import { LoginComponent } from './login.component';
import { ViewCryptosComponent } from './view-cryptos.component';
import { DashboardComponent } from './dashboard.component';

export const routes: Routes = [
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
    path: "auth",
    component: LoginComponent,
  },
];

bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes)],
}).catch((err) => console.error(err));
