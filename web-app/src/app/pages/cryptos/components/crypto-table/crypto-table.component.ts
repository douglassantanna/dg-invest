import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { Router } from '@angular/router';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
@Component({
  selector: 'app-crypto-table',
  standalone: true,
  imports: [
    CommonModule,
    PercentDifferenceComponent,
    MatIconModule,
    MatButtonModule,
    MatTableModule
  ],
  templateUrl: './crypto-table.component.html'
})
export class CryptoTableComponent {
  @Input() cryptos: ViewCryptoInformation[] = [];
  @Input() hideZeroBalance: boolean = false;
  displayedColumns: string[] = ['name', 'unitPrice', 'investedAmount', 'currentWorth', 'action'];
  router = inject(Router);
  cryptoDashboard(cryptoID: number) {
    this.router.navigate(['/crypto-dashboard', cryptoID]);
  }
}
