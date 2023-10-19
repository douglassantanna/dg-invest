import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './services/auth.service';
import { LoginFormModel } from './models/login.form-models';
import { FormDirective } from './directives/form-directive.directive';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    FormDirective],
  template: `
    <div class="container">
    <div class="row justify-content-center align-items-center min-vh-100">
      <div class="col-md-6">
        <form (ngSubmit)="login()" class="p-4 border rounded shadow" (formValueChange)="loginFormValue.set($event)">
          <h2 class="mb-4 text-center">DG Invest</h2>
          <div class="mb-3">
            <label for="email" class="form-label">Email</label>
            <input [ngModel]="loginFormValue().email" type="email" class="form-control" id="email" name="email" placeholder="Enter your email" required>
          </div>
          <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input [ngModel]="loginFormValue().password" type="password" class="form-control" id="password" name="password" placeholder="Enter your password" required>
          </div>
          <div class="text-center">
            <button
              type="submit"
              class="btn btn-primary"
              [disabled]="loading"
              [ngStyle]="{'cursor': loading ? 'not-allowed' : 'pointer'}"
              >{{loading ? 'Loading...' : 'Login'}}</button>
          </div>
        </form>
      </div>
    </div>
  </div>
  `,
  styles: [
    `
    `
  ]
})
export class LoginComponent {
  authService = inject(AuthService);
  private router = inject(Router);
  protected readonly loginFormValue = signal<LoginFormModel>({});
  loading = false;

  login() {
    this.loading = true;

    this.authService.login(this.loginFormValue()).subscribe({
      next: (value) => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        console.log(err);
        this.loading = false;
      }
    });
  }
}
