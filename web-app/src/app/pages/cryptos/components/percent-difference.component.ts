import { Component, input, Input } from '@angular/core';
import { CommonModule, NgClass } from '@angular/common';
import { DecimalRoundPipe } from 'src/app/core/pipes/decimal-round.pipe';

@Component({
  selector: 'app-percent-difference',
  standalone: true,
  imports: [NgClass, DecimalRoundPipe],
  template: `
    <span
      class="inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium transition-colors duration-200"
      [ngClass]="{
        'bg-red-50 text-red-400 dark:text-red-300 border border-red-300': percentDifference() < 0,
        'bg-green-50 text-green-400 dark:text-green-300 border border-green-300': percentDifference() > 0
      }">
      @if (percentDifference() < 0) {
        <i class="mr-1">
          <svg xmlns="http://www.w3.org/2000/svg" width="8" height="8" fill="currentColor" class="inline-block rotate-90 scale-x-[-1]" viewBox="0 0 32 32">
            <path d="M3.41 2H16V0H1a1 1 0 0 0-1 1v16h2V3.41l28.29 28.3 1.41-1.41z"/>
          </svg>
        </i>
      }
      @if (percentDifference() > 0){
        <i class="mr-1">
          <svg xmlns="http://www.w3.org/2000/svg" width="8" height="8" fill="currentColor" class="inline-block scale-x-[-1]" viewBox="0 0 32 32">
            <path d="M3.41 2H16V0H1a1 1 0 0 0-1 1v16h2V3.41l28.29 28.3 1.41-1.41z"/>
          </svg>
        </i>
      }
      {{ percentDifference() | decimalRound }}%
    </span>
  `,
})
export class PercentDifferenceComponent {
  percentDifference = input.required<number>();
}
