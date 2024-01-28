import { Injectable } from '@angular/core';
import { NavItems } from '../models/nav-items';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  navItems: NavItems[] = [
    {
      label: 'Cryptos',
      path: 'cryptos',
      icon: 'currency_bitcoin',
      roles: ['admin', 'user'],
    },
    {
      label: 'Users',
      path: 'users',
      icon: 'people',
      roles: ['admin'],
    },
    {
      label: 'Sign out',
      path: 'signout',
      icon: 'signout',
      roles: ['admin', 'user'],
    }
  ];
}
