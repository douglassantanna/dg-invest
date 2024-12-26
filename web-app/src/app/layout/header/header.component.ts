import { Role } from '../../core/models/user.model';
import { SlicePipe } from '@angular/common';
import { Component, computed, inject, OnInit, output, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { LayoutService } from '../../core/services/layout.service';
import { AuthService } from '../../core/services/auth.service';
import { NavItems } from '../../core/models/nav-items';
import { environment } from 'src/environments/environment.development';
import { ModalComponent } from '../modal/modal.component';
import { AccountSelectionComponent, SimpleAccountDto } from 'src/app/pages/cryptos/components/account-selection/account-selection.component';
import { AccountService } from 'src/app/core/services/account.service';

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
export class HeaderComponent implements OnInit {
  toggleSidenavEvent = output();
  private layoutService = inject(LayoutService);
  private authService = inject(AuthService);
  private accountService = inject(AccountService);
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
  accountTag = signal('');
  loading = signal(false);

  ngOnInit(): void {
    this.loading.set(true);
    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        this.loading.set(false);
        const selectedAccount = accounts.find((account) => account.isSelected);
        this.accountTag.set(selectedAccount?.subaccountTag ?? '');
      },
      error: (err) => {
        this.loading.set(false);
        console.error(err);
      }
    })
  }

  username = computed(() => this.authService.user?.unique_name);

  toggleMenu() {
    this.isCollapsed.set(!this.isCollapsed());
  }

  toggleAccountModal(accounts: SimpleAccountDto[]) {
    const selectedAccount = accounts.find(account => account.isSelected);
    if (selectedAccount) {
      this.accountTag.set(selectedAccount.subaccountTag);
      location.reload();
    }
    this.showAccountModal.set(!this.showAccountModal());
  }

  logout(item: NavItems) {
    if (item.path === 'signout')
      this.authService.logout();
  }
}
