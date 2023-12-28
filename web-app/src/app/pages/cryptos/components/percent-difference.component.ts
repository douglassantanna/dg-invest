import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DecimalRoundPipe } from 'src/app/core/pipes/decimal-round.pipe';

@Component({
  selector: 'app-percent-difference',
  standalone: true,
  imports: [CommonModule, DecimalRoundPipe],
  template: `
    <span
    class="mr-2 value"
    [attr.data-status]="percentDifference < 0 ? 'negative' : 'positive'">
    <i *ngIf="percentDifference < 0">
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-caret-down-fill" viewBox="0 0 16 16">
        <path d="M7.247 11.14 2.451 5.658C1.885 5.013 2.345 4 3.204 4h9.592a1 1 0 0 1 .753 1.659l-4.796 5.48a1 1 0 0 1-1.506 0z"/>
      </svg>
    </i>
    <i *ngIf="percentDifference > 0">
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" class="bi bi-caret-up-fill" viewBox="0 0 16 16">
      <path d="m7.247 4.86-4.796 5.481c-.566.647-.106 1.659.753 1.659h9.592a1 1 0 0 0 .753-1.659l-4.796-5.48a1 1 0 0 0-1.506 0z"/>
      </svg>
    </i>
      {{ percentDifference | decimalRound }}%
    </span>
  `,
  styles: [`
    .value {
      padding: 3px;
    }
    .value[data-status="negative"] {
    color: red;
    }
    .value[data-status="positive"] {
      color: green;
    }
  `]
})
export class PercentDifferenceComponent {
  @Input() percentDifference: number = 0;
}
