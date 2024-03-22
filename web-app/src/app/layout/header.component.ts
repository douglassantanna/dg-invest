import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';

import { LayoutService } from '../core/services/layout.service';
import { AuthService } from '../core/services/auth.service';
import { NavItems } from '../core/models/nav-items';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule],
  template: `
  <nav class="flex items-center justify-between bg-gray-800 text-white px-4 py-2">
    <a routerLink="/" class="text-xl font-bold">Your App Name</a>
    <ul class="flex space-x-4">
      <li><a routerLink="/about" class="hover:text-gray-400">About</a></li>
      <li><a routerLink="/contact" class="hover:text-gray-400">Contact</a></li>
      </ul>
  </nav>
  `,
})
export class HeaderComponent {
  private layoutService = inject(LayoutService);
  authService = inject(AuthService);
  isCollapsed: boolean = true;
  navItems: NavItems[] = [];

  constructor() {
    this.shouldShowLink();
  }

  toggleMenu() {
    this.isCollapsed = !this.isCollapsed;
  }

  logout(item: any) {
    if (item.path === 'signout')
      this.authService.logout();
  }

  shouldShowLink() {
    if (this.authService.role === 'user')
      this.navItems = this.layoutService.navItems.filter((item) => item.path === 'signout')
  }
}
