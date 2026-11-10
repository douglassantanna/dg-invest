import { Component, computed, input } from '@angular/core';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

// Cards that show a coin quantity rather than a USD amount.
const QUANTITY_TITLES = ['Balance', 'Without history'];

@Component({
  selector: 'app-stats-card',
  templateUrl: './stats-card.component.html',
  standalone: true,
  imports: [
    PercentDifferenceComponent,
    FormatCurrencyPipe
  ]
})
export class StatsCardComponent {
  title = input<string>('');
  value = input<number>(0);
  percentDifference = input<number>(0);
  isQuantity = computed(() => QUANTITY_TITLES.includes(this.title()));
}
