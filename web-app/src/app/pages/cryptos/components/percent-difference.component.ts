import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DecimalRoundPipe } from 'src/app/core/pipes/decimal-round.pipe';

@Component({
  selector: 'app-percent-difference',
  standalone: true,
  imports: [CommonModule, DecimalRoundPipe],
  template: `
    <span
    class="mr-2"
    [attr.data-status]="percentDifference < 0 ? 'negative' : 'positive'">
      {{ percentDifference | decimalRound }}%
    </span>
  `,
  styles: [`
  span {
    color: white;
    padding: 3px;
  }
  span[data-status="negative"] {
  background-color: red;
  }
  span[data-status="positive"] {
    background-color: green;
  }
  `]
})
export class PercentDifferenceComponent {
  @Input() percentDifference: number = 0;
}
