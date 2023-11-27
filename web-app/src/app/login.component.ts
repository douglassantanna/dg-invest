import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { ToastService } from './core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule],
  template: `
    <div class="container">
    <div class="row justify-content-center align-items-center min-vh-100">
      <div class="col-md-6">
        <form (ngSubmit)="login()" class="p-4 border rounded shadow" [formGroup]="loginForm">
          <h2 class="mb-4 text-center">DG Invest</h2>
          <div class="mb-3">
            <label for="email" class="form-label">Email</label>
            <input required formControlName="email" type="email" id="email" class="form-control" placeholder="Enter your email">
            <div style="color: red;" *ngIf="emailFormControl?.hasError('email') && !emailFormControl?.hasError('required')">
              Please enter a valid email address.
            </div>
          </div>
          <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input required formControlName="password" type="password" class="form-control" placeholder="Enter your password">
            <div style="color: red;" *ngIf="passwordFormControl?.hasError('minlength')">
              Minimun password length is 4 characters.
            </div>
          </div>
          <div class="text-center">
            <button
              type="submit"
              class="btn btn-primary"
              [disabled]="loading || loginForm.invalid"
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
  loading = false;
  loginForm!: FormGroup;

  constructor(private fb: FormBuilder, private toastService: ToastService) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  login() {
    this.loading = true;

    this.authService.login(this.loginForm.value).subscribe({
      next: (value) => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading = false;
        this.toastService.showError(err.error.message);
      }
    });
  }

  get emailFormControl() {
    return this.loginForm.get('email');
  }
  get passwordFormControl() {
    return this.loginForm.get('password');
  }
}
