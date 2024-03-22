import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from 'src/app/layout/button/button.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonComponent],
  templateUrl: 'login.component.html'
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

  submitLogin() {
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
