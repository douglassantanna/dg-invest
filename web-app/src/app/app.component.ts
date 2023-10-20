import { RouterModule } from '@angular/router';
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';
import { HeaderComponent } from './header.component';
import { ToastComponent } from './toast.component';

@Component({
  selector: 'app-root',
  template: `
      <header>
        <ng-container *ngIf="authService.isLoggedIn | async; else unAuthorized">
          <app-header />

          <router-outlet></router-outlet>
        </ng-container>
      </header>
      <main>
        <ng-template #unAuthorized>
          <router-outlet></router-outlet>
        </ng-template>
      </main>
  <router-outlet></router-outlet>
  <app-toast aria-live="polite" aria-atomic="true"></app-toast>
  `,
  standalone: true,
  imports: [RouterModule, CommonModule, HeaderComponent, ToastComponent],
})
export class AppComponent {
  authService = inject(AuthService);
}
