import { Component, input } from '@angular/core';
import { PercentDifferenceComponent } from '../percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

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
}
