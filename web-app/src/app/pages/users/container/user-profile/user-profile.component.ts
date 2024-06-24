import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { UserDecode } from 'src/app/core/models/user-decode';
import { AuthService } from 'src/app/core/services/auth.service';
import { ToastService } from 'src/app/core/services/toast.service';
import { UserService } from 'src/app/core/services/user.service';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
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
          this.toastService.showSuccess('Your profile was updated successfully');
        },
        error: () => {
          this.loading = false;
          this.toastService.showError('There was an error updating your profile');
        }
      });
    }
  }
}
