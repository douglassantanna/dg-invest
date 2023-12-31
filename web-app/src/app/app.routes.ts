import { Routes } from '@angular/router';
import { CreateCryptoComponent } from './pages/cryptos/containers/create-crypto.component';
import { CryptoDetailsComponent } from './pages/cryptos/containers/crypto-details.component';
import { DashboardComponent } from './layout/dashboard.component';
import { authGuard } from './core/guards/auth.guard';
import { LoginComponent } from './pages/auth/login.component';
import { ProfileComponent } from './pages/profile/profile.component';
import { ViewCryptosComponent } from './pages/cryptos/containers/view-cryptos.component';

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
    component: CryptoDetailsComponent,
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
