import { Injectable, signal } from '@angular/core';
import { NavItems } from '../models/nav-items';
import { Role } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  navItems: NavItems[] = [
    {
      label: 'Portfolio',
      path: 'cryptos',
      icon: 'list_alt',
      roles: [Role.Admin, Role.User],
    },
    {
      label: 'Account',
      path: 'account',
      icon: 'account_balance_wallet',
      roles: [Role.Admin, Role.User],
    },
    {
      label: 'Profile',
      path: 'user-profile',
      icon: 'manage_accounts',
      roles: [Role.Admin, Role.User],
    },
    {
      label: 'Users',
      path: 'users',
      icon: 'group',
      roles: [Role.Admin],
    },
    {
      label: 'Logout',
      path: 'logout',
      icon: 'logout',
      roles: [Role.Admin, Role.User],
    }
  ];
  private isMenuCollapsed = signal(false);
  toggleMenu() {
    this.isMenuCollapsed.set(!this.isMenuCollapsed());
  }
  isCollapsed() {
    return this.isMenuCollapsed();
  }
}
