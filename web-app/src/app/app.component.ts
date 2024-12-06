import { RouterModule } from '@angular/router';
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { HeaderComponent } from './layout/header/header.component';
import { ToastComponent } from './layout/toast.component';
import { NgxSpinnerModule } from 'ngx-spinner';
import { SidenavComponent } from './layout/sidenav/sidenav.component';

@Component({
  selector: 'app-root',
  template: `
    @if (authService.isLoggedIn | async){
      <app-sidenav [isCollapsed]="onToggleSidenav()">
        <header>
          <app-header (toggleSidenavEvent)="toggleSidenavEvent()" />
        </header>
        <div>
          <router-outlet></router-outlet>
        </div>
      </app-sidenav>
    } @else {
      <router-outlet></router-outlet>
    }
  <ngx-spinner type="ball-8bits"><h3>Loading...</h3></ngx-spinner>
  `,
  standalone: true,
  imports: [
    RouterModule,
    CommonModule,
    HeaderComponent,
    NgxSpinnerModule,
    SidenavComponent],
})
export class AppComponent {
  authService = inject(AuthService);
  onToggleSidenav = signal(false);
  toggleSidenavEvent() {
    this.onToggleSidenav.set(!this.onToggleSidenav());
  };
  constructor() {
  }
}
