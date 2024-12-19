import { Role } from '../../core/models/user.model';
import { SlicePipe } from '@angular/common';
import { Component, computed, inject, output, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { LayoutService } from '../../core/services/layout.service';
import { AuthService } from '../../core/services/auth.service';
import { NavItems } from '../../core/models/nav-items';
import { environment } from 'src/environments/environment.development';
import { ModalComponent } from '../modal/modal.component';
import { AccountSelectionComponent } from 'src/app/pages/cryptos/components/account-selection/account-selection.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    RouterModule,
    SlicePipe,
    ModalComponent,
    AccountSelectionComponent],
  templateUrl: './header.component.html',
})
export class HeaderComponent {
  toggleSidenavEvent = output();
  private layoutService = inject(LayoutService);
  private authService = inject(AuthService);
  isCollapsed = signal(true);
  showAccountModal = signal(false);
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

  toggleAccountModal() {
    this.showAccountModal.set(!this.showAccountModal());
  }

  logout(item: NavItems) {
    if (item.path === 'signout')
      this.authService.logout();
  }
}
