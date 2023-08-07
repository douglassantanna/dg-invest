import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-my-crypto',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule],
  template: `
   <mat-card>
        <h1>My {{myCrypto}} in {{myCurrency}}</h1>
        <mat-card-content>
          <div class="fields">
            <mat-form-field appearance="outline">
              <mat-label>Price per Unit</mat-label>
              <input matInput type="number"  value="29.000">
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>My avarege price</mat-label>
              <input matInput type="number"  value="22.187">
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>% difference</mat-label>
              <input matInput type="number"  value="22.187">
            </mat-form-field>
          </div>
          <div class="fields">
            <mat-form-field appearance="outline">
              <mat-label>Amount</mat-label>
              <input matInput type="number"  value="0.00148">
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Total in U$</mat-label>
              <input matInput type="number"  value="1.489">
            </mat-form-field>
          </div>
        </mat-card-content>
      </mat-card>
  `,
  styles: [`
    h1{
      padding:16px 0px 0px 16px
    }
    mat-form-field{
      width: 100%;
    }
    .fields{
      display:flex;
      gap:10px;
    }
    @media (max-width: 640px) {
        .fields{
          display:flex;
          flex-direction:column;
        }
      }
  `]
})
export class MyCryptoComponent {
  myCrypto = 'Bitcoin';
  myCurrency = 'U$';
}
