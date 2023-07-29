import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatIconModule],
  template: `
    <mat-toolbar color="primary" class="toolbar">
      <div style="display: flex; align-items:center; gap: 4px;">
        <button
          mat-button>
          <mat-icon>savings</mat-icon>
          DG Invest
        </button>
        <button
          mat-button>
          <mat-icon>currency_bitcoin</mat-icon>
          Crypto
        </button>
      </div>
      <span class="example-spacer"></span>
      <div style="display: flex; align-items:center; gap: 8px;">
        <a
          mat-button
          href="#blank"
          target="_blank"
          class="example-icon favorite-icon"
          aria-label="Example icon-button with heart icon"
        >
          <mat-icon>account_circle</mat-icon>
          Meus dados
        </a>
      </div>
    </mat-toolbar>
  `,
  styles: [
    `
    .example-spacer {
      flex: 1 1 auto;
    }
    @media (max-width: 640px) {
      .toolbar {
        flex-direction: column;
        height: auto;
      }
    }
  `,
  ],
})
export class HeaderComponent {

}
