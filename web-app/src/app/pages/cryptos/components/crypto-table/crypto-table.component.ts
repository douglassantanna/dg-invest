import { Component, Input, computed, inject, input, output, signal } from '@angular/core';
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
  sortOrder = input<string>('');
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
    if (event == 'symbol' || event == 'invested_amount')
      this.outputHeader.emit(event);
  }

  defineSortHeaderArrow(): string {
    return this.sortOrder() === 'asc' ? '⬆' : '⬇'
  }
  isValidTableHeader(value: string): boolean {
    return value === 'symbol' || value === 'invested_amount';
  }
}
