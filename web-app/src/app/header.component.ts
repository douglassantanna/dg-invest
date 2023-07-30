import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { LayoutService } from './services/layout.service';
import { pluck } from 'rxjs';
import { AuthService } from './services/auth.service';
import { RouterModule } from '@angular/router';
import { MatTooltipModule } from '@angular/material/tooltip';

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
            [routerLink]="[item.path]"
            mat-button
            routerLinkActive="active">
            <mat-icon class="mr-2">{{ item.icon }}</mat-icon>
            <span>{{ item.label }}</span>
          </button>
        </ng-container>
      </div>
      <span class="example-spacer"></span>
      <div style="display: flex; align-items:center; gap: 8px;">
        <a
          mat-button
          href="#blank"
          target="_blank"
          class="example-icon favorite-icon"
          aria-label="Example icon-button with heart icon"
        >
          <mat-icon>account_circle</mat-icon>
          Meus dados
        </a>
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
  layoutService = inject(LayoutService);
  authService = inject(AuthService);

  navItems = this.layoutService.navItems;

  name$ = this.authService.auth$.pipe(pluck('name'));
}
