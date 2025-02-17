import { NgClass } from '@angular/common';
import { Component, computed, HostListener, inject, input } from '@angular/core';
import { RouterModule } from '@angular/router';
import { NavItems } from 'src/app/core/models/nav-items';
import { Role } from 'src/app/core/models/user.model';
import { AuthService } from 'src/app/core/services/auth.service';
import { LayoutService } from 'src/app/core/services/layout.service';

@Component({
  selector: 'app-sidenav',
  templateUrl: './sidenav.component.html',
  styles: ``,
  standalone: true,
  imports: [RouterModule, NgClass],
})
export class SidenavComponent {
  private authService = inject(AuthService);
  private layoutService = inject(LayoutService);
  isCollapsed = computed(() => this.layoutService.isCollapsed());
  navItemsEnabled = computed(() => {
    const userRole = this.authService.role as Role;
    const filteredNavItems = this.layoutService.navItems.filter(item => item.roles.some(role => role === userRole));
    return filteredNavItems;
  }
  );

  @HostListener('window:resize', ['$event'])
  onResize() {
    this.layoutService.toggleMobileMode(window.innerWidth);
  }

  logout(item: NavItems) {
    if (item.path === 'logout')
      this.authService.logout();
  }

  isMobile() {
    return this.layoutService.isMobile();
  }
}
