import { Routes } from '@angular/router';
import { CreateCryptoComponent } from './pages/cryptos/containers/create-crypto.component';
import { CryptoDetailsComponent } from './pages/cryptos/containers/crypto-details.component';
import { authGuard } from './core/guards/auth.guard';
import { LoginComponent } from './pages/auth/login.component';
import { ViewCryptosComponent } from './pages/cryptos/containers/view-cryptos/view-cryptos.component';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: "",
    pathMatch: "full",
    redirectTo: "cryptos",
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
    path: "user-profile",
    canActivate: [authGuard],
    loadComponent: () => import('./pages/users/container/user-profile/user-profile.component').then((c) => c.UserProfileComponent),
  },
  {
    path: "users",
    canActivate: [authGuard, roleGuard],
    loadComponent: () => import('./pages/users/container/view-users/view-users.component').then((c) => c.ViewUsersComponent),
  },
];
