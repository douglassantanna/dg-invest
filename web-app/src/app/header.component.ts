import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';

import { LayoutService } from './services/layout.service';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule],
  template: `
    <nav class="navbar navbar-expand-lg bg-primary" data-bs-theme="dark">
      <div class="container-fluid">
        <a class="navbar-brand" href="#">DG</a>
        <button class="navbar-toggler" type="button" (click)="toggleMenu()" aria-controls="navbarNavAltMarkup" aria-expanded="false" aria-label="Toggle navigation">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" [ngClass]="{'collapse': isCollapsed}" id="navbarNavAltMarkup">
        <div class="navbar-nav" *ngFor="let item of navItems">
            <a class="nav-link" aria-current="page" [routerLink]="item.path"  (click)="logout(item)">{{ item.label }}</a>
          </div>
        </div>
      </div>
    </nav>
  `,
  styles: [
    `
  `,
  ],
})
export class HeaderComponent {
  isCollapsed: boolean = true;

  toggleMenu() {
    this.isCollapsed = !this.isCollapsed;
  }

  private layoutService = inject(LayoutService);
  authService = inject(AuthService);

  navItems = this.layoutService.navItems;

  logout(item: any) {
    if (item.path === 'signout')
      this.authService.logout();
  }
}
