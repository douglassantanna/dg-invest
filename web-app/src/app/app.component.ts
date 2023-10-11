import { RouterModule } from '@angular/router';
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';
import { HeaderComponent } from './header.component';

@Component({
  selector: 'app-root',
  template: `
      <header>
        <ng-container *ngIf="authService.token; else unAuthorized">
          <app-header />

          <router-outlet></router-outlet>
        </ng-container>
      </header>
      <main>
        <ng-template #unAuthorized>
          <router-outlet></router-outlet>
        </ng-template>
      </main>
  <router-outlet></router-outlet>`,
  standalone: true,
  imports: [RouterModule, CommonModule, HeaderComponent],
})
export class AppComponent {
  authService = inject(AuthService);
  title = 'web-app';
}
