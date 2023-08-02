import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule],
  template: `
    <div class="profile-pic">
      <img src="assets/bart-simpson.png" alt="bart simpson profile picture">
    </div>
    <div class="container">
      <form>
        <mat-form-field appearance="outline">
          <mat-label>Full name</mat-label>
          <input matInput type="text"  value="Douglas Sant'Anna Figueredo">
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>E-mail</mat-label>
          <input matInput type="text"  value="douglbb1@gmail.com">
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Trust Wallet address</mat-label>
          <input matInput type="text"  value="kansckjns-asknxken43d-ckwjenc-3dweni">
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Phone number</mat-label>
          <input matInput type="text"  value="11 94101-2994">
        </mat-form-field>

        <button mat-raised-button color="primary">Save</button>
      </form>
    </div>
  `,
  styles: [`
  button{
    margin-left:auto;
  }
  mat-form-field{
    width:100%;
  }
  .container{
    display: flex;
    justify-content: center;
    align-items: center;
    background-color:white;
    padding:20px;
  }
  form{
    display: flex;
    justify-content: center;
    flex-direction: column;
    width:100%;
    margin-top:50px;
  }
  img{
    width:150px;
    background-color:white;
    border-radius:100%;
    /* top:-40; */
  }
  .profile-pic{
    display: flex;
    justify-content: center;
    align-items: center;
  }
  `]
})
export class ProfileComponent {

}
