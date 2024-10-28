import { Injectable } from '@angular/core';
import { NavItems } from '../models/nav-items';
import { Role } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  navItems: NavItems[] = [
    {
      label: 'Cryptos',
      path: 'cryptos',
      icon: 'currency_bitcoin',
      roles: [Role.Admin, Role.User],
    },
    {
      label: 'Users',
      path: 'users',
      icon: 'people',
      roles: [Role.Admin],
    },
    {
      label: 'My Profile',
      path: 'user-profile',
      icon: 'profile',
      roles: [Role.Admin, Role.User],
    },
    {
      label: 'Sign out',
      path: 'signout',
      icon: 'signout',
      roles: [Role.Admin, Role.User],
    }
  ];
}
