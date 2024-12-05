import { Injectable } from '@angular/core';
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
      label: 'Users',
      path: 'users',
      icon: 'group',
      roles: [Role.Admin],
    },
    {
      label: 'Profile',
      path: 'user-profile',
      icon: 'manage_accounts',
      roles: [Role.Admin, Role.User],
    },
    {
      label: 'Log out',
      path: 'logout',
      icon: 'logout',
      roles: [Role.Admin, Role.User],
    }
  ];
}
