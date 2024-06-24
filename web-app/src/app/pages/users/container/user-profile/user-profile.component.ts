import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { UserDecode } from 'src/app/core/models/user-decode';
import { AuthService } from 'src/app/core/services/auth.service';

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
    console.log(this.userFullname)
    console.log(this.userEmail)
  }
}
