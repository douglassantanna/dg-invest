import { LoadingSpinnerComponent } from './../../../layout/loading-spinner/loading-spinner.component';
import { NgStyle } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { tap } from 'rxjs';
import { environment } from 'src/environments/environment.development';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    NgStyle,
    ReactiveFormsModule,
    LoadingSpinnerComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  authService = inject(AuthService);
  private router = inject(Router);
  loading = signal(false);
  loginForm!: FormGroup;
  btnColor = environment.btnColor;
  errorMessages = signal<string[]>([]);

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  ngOnInit(): void {
    this.checkLoginStatus();
  }

  login() {
    this.loading.set(true);
    this.errorMessages.set([]);

    this.authService.login(this.loginForm.value).subscribe({
      next: (value) => {
        this.loading.set(false);
        this.router.navigate(['/cryptos']);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessages.set([err.error.message]);
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
