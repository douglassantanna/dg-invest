import { RouterModule } from '@angular/router';
import { Component, inject, signal } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { HeaderComponent } from './layout/header/header.component';
import { SidenavComponent } from './layout/sidenav/sidenav.component';
import { LoadingSpinnerComponent } from './layout/loading-spinner/loading-spinner.component';

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
      <app-loading-spinner />
    } @else {
      <router-outlet></router-outlet>
    }
  `,
  standalone: true,
  imports: [
    RouterModule,
    AsyncPipe,
    HeaderComponent,
    SidenavComponent,
    LoadingSpinnerComponent],
})
export class AppComponent {
  authService = inject(AuthService);
  onToggleSidenav = signal(false);
  toggleSidenavEvent() {
    this.onToggleSidenav.set(!this.onToggleSidenav());
  };
}
