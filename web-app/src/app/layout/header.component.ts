import { Role } from './../core/models/user.model';
import { NgClass, SlicePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { LayoutService } from '../core/services/layout.service';
import { AuthService } from '../core/services/auth.service';
import { NavItems } from '../core/models/nav-items';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    NgClass,
    RouterModule,
    SlicePipe],
  template: `
    <nav class="navbar navbar-expand-lg" [ngClass]="navbarColor" data-bs-theme="dark">
      <div class="container-fluid">
        @if (username()) {
          <a class="navbar-brand username-initial-letter">{{ username() | slice: 0: 1 }}</a>
        }

        <button class="navbar-toggler" type="button" (click)="toggleMenu()" aria-controls="navbarNavAltMarkup" aria-expanded="false" aria-label="Toggle navigation">
          <span class="navbar-toggler-icon"></span>
        </button>

        <div class="collapse navbar-collapse" [ngClass]="{'collapse': isCollapsed()}" id="navbarNavAltMarkup">
          <div class="navbar-nav">
          @for (item of navItemsEnabled(); track $index) {
                  <a class="nav-link" aria-current="page" [routerLink]="item.path" (click)="logout(item)">{{ item.label }}</a>
                }
          </div>
          <span class="navbar-text ms-auto me-3">Welcome, <b>{{ username() }}</b></span>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .username-initial-letter{
      border: white 1px solid;
      padding:5px;
      border-radius:50px;
      color:white;
      width:30px;
      height:30px;
      display:flex;
      align-items:center;
      justify-content:center
    }
    `]
})
export class HeaderComponent {
  private layoutService = inject(LayoutService);
  private authService = inject(AuthService);
  isCollapsed = signal(true);
  navItems = signal<NavItems[]>([]);
  navbarColor = environment.navbarColor;
  navItemsEnabled = computed(() => {
    const userRole = this.authService.role as Role;
    const filteredNavItems = this.layoutService.navItems.filter(item => item.roles.some(role => role === userRole));
    return filteredNavItems;
  }
  );
  username = computed(() => this.authService.user?.unique_name);

  toggleMenu() {
    this.isCollapsed.set(!this.isCollapsed());
  }

  logout(item: NavItems) {
    if (item.path === 'signout')
      this.authService.logout();
  }
}
