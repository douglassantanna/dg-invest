import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';

import { LayoutService } from './services/layout.service';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    CommonModule,
    RouterModule,
    MatTooltipModule],
  template: `
    <mat-toolbar color="primary" class="toolbar">
      <div style="display: flex; align-items:center; gap: 4px;">
        <ng-container *ngFor="let item of navItems">
          <button
            class="mr-2"
            (click)="navigate(item.path)"
            mat-button
            routerLinkActive="active">
            <mat-icon class="mr-2">{{ item.icon }}</mat-icon>
            <span>{{ item.label }}</span>
          </button>
        </ng-container>
      </div>
      <span class="example-spacer"></span>
      <div style="display: flex; align-items:center; gap: 8px;">
        <button
          mat-button
          (click)="navigate('profile')"
        >
          <mat-icon>account_circle</mat-icon>
          Meus dados
        </button>
        <button
          mat-button
          type="button"
          (click)="logout()">
          <mat-icon>logout</mat-icon>
          Sair
        </button>
      </div>
    </mat-toolbar>
  `,
  styles: [
    `
    .example-spacer {
      flex: 1 1 auto;
    }
    @media (max-width: 640px) {
      .toolbar {
        flex-direction: column;
        height: auto;
      }
    }
  `,
  ],
})
export class HeaderComponent {
  private layoutService = inject(LayoutService);
  private router = inject(Router);
  authService = inject(AuthService);

  navItems = this.layoutService.navItems;

  navigate(route: string) {
    this.router.navigate([route]);
  }
  logout() {
    this.authService.fakeLogout();
    this.router.navigate(['/login']);
  }
}
