import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  navItems = [
    {
      label: 'DG Invest',
      path: '',
      icon: 'savings',
      roles: ['admin', 'manager', 'user'],
    },
    {
      label: 'Cryptos',
      path: 'cryptos',
      icon: 'currency_bitcoin',
      roles: ['admin', 'manager', 'user'],
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
