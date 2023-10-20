import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

import { LayoutService } from './services/layout.service';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule],
  template: `
    <nav class="navbar navbar-expand-lg navbar-light bg-light">
      <div class="container">
        <a class="navbar-brand" href="#">DG</a>
        <button
          class="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target="#navbarNav"
        >
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse" id="navbarNav">
          <ul class="navbar-nav">
            <li
              class="nav-item"
              *ngFor="let item of navItems"
              [ngClass]="{ 'd-none': authService.role }"
            >
                <a class="nav-link" [routerLink]="item.path">{{ item.label }}</a>
            </li>
          </ul>
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
  private layoutService = inject(LayoutService);
  private router = inject(Router);
  authService = inject(AuthService);

  navItems = this.layoutService.navItems;

  navigate(route: string) {
    this.router.navigate([route]);
  }
  logout() {
    this.authService.logout();
  }
}
