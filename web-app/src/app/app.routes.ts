import { Routes } from '@angular/router';
import { CryptoDetailsComponent } from './pages/cryptos/containers/crypto-details/crypto-details.component';
import { authGuard } from './core/guards/auth.guard';
import { LoginComponent } from './pages/auth/login/login.component';
import { ViewCryptosComponent } from './pages/cryptos/containers/view-cryptos/view-cryptos.component';
import { roleGuard } from './core/guards/role.guard';
import { AccountComponent } from './pages/cryptos/containers/account/account.component';
import { DepositComponent } from './pages/cryptos/containers/deposit/deposit.component';
import { WithdrawComponent } from './pages/cryptos/containers/withdraw/withdraw.component';
import { ExchangeIndexComponent } from './pages/exchanges/containers/exchange-index/exchange-index.component';
import { BybitIntegrationComponent } from './pages/exchanges/containers/bybit-integration/bybit-integration.component';
import { BybitAccountComponent } from './pages/exchanges/containers/bybit-account/bybit-account.component';

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
    path: "exchanges",
    canActivate: [authGuard],
    component: ExchangeIndexComponent,
  },
  {
    path: "exchanges/bybit/:accountId",
    canActivate: [authGuard],
    component: BybitAccountComponent,
  },
  {
    path: "exchanges/bybit",
    canActivate: [authGuard],
    component: BybitIntegrationComponent,
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
