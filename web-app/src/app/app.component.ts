import { RouterModule } from '@angular/router';
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from './core/services/auth.service';
import { HeaderComponent } from './layout/header.component';
import { ToastComponent } from './layout/toast.component';
import { NgxSpinnerModule } from 'ngx-spinner';

@Component({
  selector: 'app-root',
  template: `
    <div *ngIf="(authService.isLoggedIn | async); else unAuthorized" >
      <app-header />

      <router-outlet></router-outlet>

      <aside class="fixed left-0 top-0 h-screen w-64 m-0 flex flex-col bg-gray-700 text-white overflow-auto">
        <div class="flex items-center justify-between p-4">
          <a routerLink="/" class="text-xl font-bold">Your App Name</a>
          <button (click)="toggleMenu()">
            <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none">
              <path d="M6 18L18 6M6 6L18 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path>
            </svg>
          </button>
        </div>
        <ul class="flex flex-col mt-4 p-4">
          <li><a routerLink="/dashboard" class="hover:text-gray-400">Dashboard</a></li>
          <li><a routerLink="/settings" class="hover:text-gray-400">Settings</a></li>
          </ul>
      </aside>

    </div>
    <div>
      <ng-template #unAuthorized>
        <router-outlet></router-outlet>
      </ng-template>
    </div>
  <app-toast aria-live="polite" aria-atomic="true"></app-toast>
  <ngx-spinner type="ball-8bits"><h3>Loading...</h3></ngx-spinner>
  `,
  standalone: true,
  imports: [
    RouterModule,
    CommonModule,
    HeaderComponent,
    ToastComponent,
    NgxSpinnerModule],
})
export class AppComponent {
  authService = inject(AuthService);
  constructor() {
  }
  isOpen = true;

  toggleMenu() {
    this.isOpen = !this.isOpen;
  }
}
