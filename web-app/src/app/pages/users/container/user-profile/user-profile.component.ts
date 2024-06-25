import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from 'src/app/core/services/auth.service';
import { ToastService } from 'src/app/core/services/toast.service';
import { UserService } from 'src/app/core/services/user.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule],
  templateUrl: './user-profile.component.html'
})
export class UserProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private toastService = inject(ToastService);
  loading = false;
  userFullname = '';
  userEmail = '';
  updatePasswordModel = signal({
    userId: 0,
    currentPassword: '',
    newPassword: '',
    confirmNewPassword: '',
  });
  hasUpperCase = signal<boolean>(false);
  hasLowerCase = signal<boolean>(false);
  hasNumbers = signal<boolean>(false);
  hasSpecialChars = signal<boolean>(false);
  hasSixChars = signal<boolean>(false);
  ngOnInit(): void {
    if (this.authService.user) {
      this.userFullname = this.authService.user.unique_name;
      this.userEmail = this.authService.user.email;
    }
  }

  updateUserProfile() {
    if (this.userEmail !== this.authService.user?.email
      || this.userFullname !== this.authService.user.unique_name
    ) {
      this.loading = true;
      this.userService.updateUserProfile(this.userFullname, this.userEmail, this.authService.user?.nameid!).subscribe({
        next: () => {
          this.loading = false;
          this.toastService.showSuccess('Your profile was updated successfully. Please, log in again.');
          this.authService.logout();
        },
        error: () => {
          this.loading = false;
          this.toastService.showError('There was an error updating your profile.');
        }
      });
    }
  }

  updatePassword() {
    this.updatePasswordModel().userId = Number(this.authService.user?.nameid!);
    console.log(this.updatePasswordModel())
  }

  passwordChecker() {
    const newPassword = this.updatePasswordModel().newPassword;
    this.hasSixChars.set(newPassword.length >= 6);
    this.hasUpperCase.set(/[A-Z]/.test(newPassword));
    this.hasLowerCase.set(/[a-z]/.test(newPassword));
    this.hasNumbers.set(/\d/.test(newPassword));
    this.hasSpecialChars.set(/[!@#$%^&*(),.?":{}|<>]/.test(newPassword));
  }
}
