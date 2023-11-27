import { RouterModule } from '@angular/router';
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';
import { HeaderComponent } from './layout/header.component';
import { ToastComponent } from './toast.component';

@Component({
  selector: 'app-root',
  template: `
  <div>
    <ng-container *ngIf="(authService.isLoggedIn | async); else unAuthorized">
      <app-header />
      <router-outlet></router-outlet>
    </ng-container>
  </div>
  <div>
    <ng-template #unAuthorized>
      <router-outlet></router-outlet>
    </ng-template>
  </div>
  <app-toast aria-live="polite" aria-atomic="true"></app-toast>
  `,
  standalone: true,
  imports: [RouterModule, CommonModule, HeaderComponent, ToastComponent],
})
export class AppComponent {
  authService = inject(AuthService);
  constructor() {
  }
}
