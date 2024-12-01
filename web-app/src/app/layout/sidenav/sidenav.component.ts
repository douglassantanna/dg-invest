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
  imports: [NgClass, RouterModule]
})
export class SidenavComponent {
  private authService = inject(AuthService);
  private layoutService = inject(LayoutService);
  private isOpen = false;
  private isMobileView = false;
  isCollapsed = input.required<boolean>();
  navItemsEnabled = computed(() => {
    const userRole = this.authService.role as Role;
    const filteredNavItems = this.layoutService.navItems.filter(item => item.roles.some(role => role === userRole));
    return filteredNavItems;
  }
  );

  @HostListener('window:resize', ['$event'])
  onResize() {
    this.isMobileView = window.innerWidth <= 600;
  }

  ngOnInit() {
    this.isMobileView = window.innerWidth <= 600;
  }

  toggleMenu() {
    this.isOpen = !this.isOpen;
  }

  logout(item: NavItems) {
    if (item.path === 'signout')
      this.authService.logout();
  }

  isMobile() {
    return this.isMobileView;
  }
}
