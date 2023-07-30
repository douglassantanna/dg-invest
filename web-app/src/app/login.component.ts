import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    FormsModule,
    MatButtonModule,
    MatProgressBarModule],
  template: `
    <div class="main">
    <div class="left-container">
      <img src="assets/finance_illustration.png" alt="finance illustration image" />
    </div>
    <div class="right-container">
        <mat-card class="mat-elevation-z1">
            <mat-card-header>
              <mat-card-title>
                Bem vindo à DG Invest.
                Acesse com seu email e senha.
              </mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <form (ngSubmit)="login()" >
                <mat-form-field appearance="outline">
                  <mat-label>email</mat-label>
                  <input matInput [(ngModel)]="userAuth.email" name="email" required type="email"/>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>senha</mat-label>
                  <input
                    matInput
                    [(ngModel)]="userAuth.password"
                    type="password"
                    name="password"
                    required
                  />
                </mat-form-field>
              </form>
            </mat-card-content>
            <mat-card-actions align="end">
              <button
                mat-raised-button
                color="primary"
                type="submit"
                (click)="login()"
                [disabled]="loading"
                >Acessar</button>
            </mat-card-actions>
            <mat-card-footer>
                <a href="#forgot-password">Esqueci minha senha</a>
                <mat-progress-bar mode="indeterminate" *ngIf="loading"></mat-progress-bar>
            </mat-card-footer>
        </mat-card>
    </div>
  </div>
  `,
  styles: [
    `
    .main {
      display: flex;
      justify-content: center;
      align-items:center;
    }
    img {
      width: 43.75rem;
    }
    mat-form-field {
      width: 100%;
    }
    a {
      text-decoration: none;
      color: inherit;
    }
    mat-card{
      display:flex;
      gap:10px;
      padding:10px;
    }
    @media (max-width: 640px) {
        .main {
          flex-direction: column;
          height: auto;
        }
        img {
          width: 22rem;
        }
      }
    `
  ]
})
export class LoginComponent {
  loading = false;
  private router = inject(Router);
  userAuth = {
    password: '',
    email: ''
  }
  login() {
    this.loading = true;
    setTimeout(() => {
      this.router.navigate(['/dashboard']);
    }, 2000);
  }
}
