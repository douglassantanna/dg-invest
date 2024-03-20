import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule],
  template: `
  <div class="flex items-center justify-center h-screen">
    <div class="w-full p-6 bg-white rounded-md shadow-md ring-2 ring-gray-800/50 sm:max-w-md">
      <h1 class="text-3xl font-semibold text-center text-gray-700 mb-4">DG Invest</h1>
      <form (ngSubmit)="login()" class="space-y-4" [formGroup]="loginForm">
        <div>
          <label for="email" class="block text-sm text-gray-600 font-semibold">Email</label>
          <input required formControlName="email" type="email" id="email"
          class="w-full px-4 py-2 border rounded-md focus:outline-none focus:border-blue-500"
          placeholder="Enter your email">
          <div class="text-red-500 text-sm" *ngIf="emailFormControl?.hasError('email') && !emailFormControl?.hasError('required')">
            Please enter a valid email address.
          </div>
        </div>
        <div>
          <label for="password" class="block text-sm text-gray-600 font-semibold">Password</label>
          <input required formControlName="password" type="password"
          class="w-full px-4 py-2 border rounded-md focus:outline-none focus:border-blue-500"
          placeholder="Enter your password">
          <div class="text-red-500 text-sm" *ngIf="passwordFormControl?.hasError('minlength')">
            Minimun password length is 4 characters.
          </div>
        </div>
        <div>
          <button
            *ngIf="!loading;else load"
            type="submit"
            class="btn btn-block btn-accent"
            [disabled]="loading || loginForm.invalid"
            [ngStyle]="{'cursor': loading ? 'not-allowed' : 'pointer'}"
            >Login
          </button>
            <ng-template #load>
              <button
              type="button"
              class="btn btn-block btn-accent"
              [disabled]="true"
              >
              <span class="pl-2 loading loading-spinner loading-sm"></span>
              </button>
            </ng-template>
        </div>
      </form>
    </div>
  </div>
  `,
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
        this.router.navigate(['/cryptos']);
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
