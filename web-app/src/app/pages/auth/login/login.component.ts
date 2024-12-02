import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { tap } from 'rxjs';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {

  authService = inject(AuthService);
  private router = inject(Router);
  loading = false;
  loginForm!: FormGroup;
  btnColor = environment.btnColor;

  constructor(private fb: FormBuilder, private toastService: ToastService) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  ngOnInit(): void {
    this.checkLoginStatus();
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

  private checkLoginStatus() {
    this.authService.isLoggedIn.pipe(tap((result) => {
      if (result) {
        this.router.navigate(['/cryptos']);
      }
    })).subscribe();
  }

  get emailFormControl() {
    return this.loginForm.get('email');
  }
  get passwordFormControl() {
    return this.loginForm.get('password');
  }
}
