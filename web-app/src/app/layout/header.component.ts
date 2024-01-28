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
    <nav class="navbar navbar-expand-lg bg-primary" data-bs-theme="dark">
      <div class="container-fluid">
        <a class="navbar-brand">DG</a>
        <button class="navbar-toggler" type="button" (click)="toggleMenu()" aria-controls="navbarNavAltMarkup" aria-expanded="false" aria-label="Toggle navigation">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" [ngClass]="{'collapse': isCollapsed}" id="navbarNavAltMarkup">
          <div class="navbar-nav" *ngFor="let item of navItems">
            <a class="nav-link" aria-current="page" [routerLink]="item.path" (click)="logout(item)">{{ item.label }}</a>
          </div>
        </div>
      </div>
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
