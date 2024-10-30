import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { LoginComponent } from './pages/auth/login.component';
import { AccountComponent } from './pages/cryptos/containers/account/account.component';
import { CreateCryptoComponent } from './pages/cryptos/containers/create-crypto.component';
import { CryptoDetailsComponent } from './pages/cryptos/containers/crypto-details.component';
import { DepositComponent } from './pages/cryptos/containers/deposit/deposit.component';
import { ViewCryptosComponent } from './pages/cryptos/containers/view-cryptos/view-cryptos.component';
import { WithdrawComponent } from './pages/cryptos/containers/withdraw/withdraw.component';

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
    path: "account",
    canActivate: [authGuard],
    component: AccountComponent,
  },
  {
    path: "account/deposit",
    canActivate: [authGuard],
    component: DepositComponent,
  },
  {
    path: "account/withdraw",
    canActivate: [authGuard],
    component: WithdrawComponent,
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
  { path: '', pathMatch: 'full', redirectTo: '/welcome' },
  { path: 'welcome', loadChildren: () => import('./pages/welcome/welcome.routes').then(m => m.WELCOME_ROUTES) }
];
