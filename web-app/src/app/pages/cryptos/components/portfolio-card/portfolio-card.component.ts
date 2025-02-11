import { CurrencyPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { PercentDifferenceComponent } from '../percent-difference.component';

@Component({
  selector: 'app-portfolio-card',
  templateUrl: './portfolio-card.component.html',
  standalone: true,
  imports: [CurrencyPipe, PercentDifferenceComponent],
})
export class PortfolioCardComponent {
  accountBalance = input.required<number>();
  totalInvested = input.required<number>();
  totalMarketValue = input.required<number>();
  investmentChangePercent = input.required<number>();
}
