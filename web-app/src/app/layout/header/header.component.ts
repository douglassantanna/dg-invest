import { Role } from '../../core/models/user.model';
import { CommonModule, NgFor, SlicePipe } from '@angular/common';
import { Component, computed, inject, output, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { LayoutService } from '../../core/services/layout.service';
import { AuthService } from '../../core/services/auth.service';
import { NavItems } from '../../core/models/nav-items';
import { environment } from 'src/environments/environment.development';
import { ModalComponent } from '../modal/modal.component';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    RouterModule,
    SlicePipe,
    ModalComponent,
    CommonModule,
    FormsModule],
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

  accounts = [
    { name: 'Main', balance: 1000 },
    { name: 'Dad', balance: 500 }
  ];
  selectedAccount = this.accounts[0];
  showNewAccountInput = false;
  newAccountName = '';
  errorMessage = '';
  selectAccount(account: any) {
    this.selectedAccount = account;
  }

  toggleNewAccountInput() {
    this.showNewAccountInput = !this.showNewAccountInput;
    this.errorMessage = '';
  }

  saveNewAccount() {
    if (this.newAccountName.trim()) {
      const duplicate = this.accounts.some(account => account.name.toLowerCase() === this.newAccountName.trim().toLowerCase());
      if (duplicate) {
        this.errorMessage = 'Account name already exists.';
      } else {
        this.accounts.push({ name: this.newAccountName, balance: 0 });
        this.newAccountName = '';
        this.showNewAccountInput = false;
        this.errorMessage = ''; // Clear error message on successful save
      }
    }
  }

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
