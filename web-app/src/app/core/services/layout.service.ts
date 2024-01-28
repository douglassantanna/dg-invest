import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  navItems = [
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
      roles: ['admin', 'user'],
    }
  ]
    ;
  constructor() { }
}
