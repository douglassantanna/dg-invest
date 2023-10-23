import { Routes } from '@angular/router';
import { CreateCryptoComponent } from './create-crypto.component';
import { CryptoDashboardComponent } from './crypto-dashboard.component';
import { DashboardComponent } from './dashboard.component';
import { authGuard } from './guards/auth.guard';
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
    canActivate: [authGuard],
    component: DashboardComponent,
  },
  {
    path: "cryptos",
    canActivate: [authGuard],
    component: ViewCryptosComponent,
  },
  {
    path: "create-crypto",
    canActivate: [authGuard],
    component: CreateCryptoComponent,
  },
  {
    path: "crypto-dashboard/:cryptoId",
    canActivate: [authGuard],
    component: CryptoDashboardComponent,
  },
  {
    path: "login",
    component: LoginComponent,
  },
  {
    path: "profile",
    canActivate: [authGuard],
    component: ProfileComponent,
  },
];
