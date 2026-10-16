import { Role } from '../../core/models/user.model';
import { SlicePipe } from '@angular/common';
import { Component, computed, inject, OnInit, output } from '@angular/core';

import { LayoutService } from '../../core/services/layout.service';
import { AuthService } from '../../core/services/auth.service';
import { NavItems } from '../../core/models/nav-items';
import { environment } from 'src/environments/environment.development';
import { AccountSelectorStateService } from 'src/app/core/services/account-selector-state.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [SlicePipe],
  templateUrl: './header.component.html',
})
export class HeaderComponent implements OnInit {
  toggleSidenavEvent = output();
  private layoutService = inject(LayoutService);
  private authService = inject(AuthService);
  private accountState = inject(AccountSelectorStateService);
  navbarColor = environment.navbarColor;
  navItemsEnabled = computed(() => {
    const userRole = this.authService.role as Role;
    const filteredNavItems = this.layoutService.navItems.filter(item => item.roles.some(role => role === userRole));
    return filteredNavItems;
  }
  );
  accountTag = computed(() => {
    const selected = this.accountState.selectedAccount();
    if (selected) return selected.accountName;
    if (this.accountState.isLoading()) return 'Loading account…';
    return this.accountState.errorMessage() ? 'Account unavailable' : 'No account selected';
  });
  username = computed(() => this.authService.user?.unique_name);

  ngOnInit(): void {
    this.accountState.load().subscribe({ error: () => undefined });
  }

  logout(item: NavItems) {
    if (item.path === 'signout')
      this.authService.logout();
  }
}
