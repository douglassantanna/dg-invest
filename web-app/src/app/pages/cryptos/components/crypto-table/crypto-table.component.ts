import { Component, Input, inject, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { Router } from '@angular/router';
import { ViewCryptoInformation } from 'src/app/core/models/view-crypto-information';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

@Component({
  selector: 'app-crypto-table',
  standalone: true,
  imports: [
    CommonModule,
    PercentDifferenceComponent,
    FormatCurrencyPipe],
  templateUrl: './crypto-table.component.html',
  styleUrls: ['./crypto-table.component.scss']
})
export class CryptoTableComponent {
  outputHeader = output<string>();
  @Input() cryptos: ViewCryptoInformation[] = [];
  @Input() hideZeroBalance: boolean = false;
  router = inject(Router);
  tableHeaders: any[] = [
    { value: 'symbol', description: 'Symbol' },
    { value: 'unit_price', description: 'Unit Price' },
    { value: 'invested_amount', description: 'Invested Amount' },
    { value: 'current_worth', description: 'Current Worth' },
    { value: 'action', description: 'Action' },
  ]
  cryptoDashboard(cryptoID: number) {
    this.router.navigate(['/crypto-dashboard', cryptoID]);
  }

  sortTable(event: any) {
    if (event != 'action')
      this.outputHeader.emit(event);
  }
}
