import { Routes } from '@angular/router';

import { CreateCryptoComponent } from './create-crypto.component';
import { CryptoDashboardComponent } from './crypto-dashboard.component';
import { DashboardComponent } from './dashboard.component';
import { LoginComponent } from './login.component';
import { ProfileComponent } from './profile.component';
import { ViewCryptosComponent } from './view-cryptos.component';

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
