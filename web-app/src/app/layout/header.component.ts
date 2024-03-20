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
    <div class="navbar bg-base-100">
      <button class="btn btn-primary text-xl">DG</button>
    </div>
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
