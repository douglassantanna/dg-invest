import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { DecimalRoundPipe } from '../../../core/pipes/decimal-round.pipe';
import { PercentDifferenceComponent } from './percent-difference.component';
import { FormatCurrencyPipe } from 'src/app/core/pipes/format-currency.pipe';

@Component({
  selector: 'app-data-card',
  standalone: true,
  imports: [
    CommonModule,
    DecimalRoundPipe,
    PercentDifferenceComponent,
    FormatCurrencyPipe],
  template: `
    <div class="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm hover:shadow-md transition-shadow duration-300">
  <div class="p-6">
    <h4 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">{{ title }}</h4>
    <div class="flex items-center justify-between">
      @if (title !== 'Balance') {
        <span class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ value | formatCurrency }}</span>
      }
      <ng-template #balance>
        <span class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ value }}</span>
      </ng-template>
      <app-percent-difference
        *ngIf="percentDifference"
        [percentDifference]="percentDifference"
        class="ml-2">
      </app-percent-difference>
    </div>
  </div>
</div>
  `,
  styles: [`
      .card {
      background-color: #f8f9fa;
      color: #343a40;
      border: 1px solid #dee2e6;
      border-radius: 0.25rem;
      margin-bottom: 1rem;
      padding: 1rem;
    }

    .card-body {
      display: flex;
      flex-direction: column;
    }

    .card-title {
      font-size: 1.5rem;
      white-space: nowrap;
      margin-bottom: 1rem;
    }

    .content {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .price-text {
      font-size: 22px;
      color: purple;
    }

    @media (min-width: 600px) {
      .card-container {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 16px;
      }
    }

    @media (max-width: 599px) {
      .card-container {
        display: grid;
        grid-template-columns: 1fr;
        gap: 16px;
      }
    }
  `]
})
export class DataCardComponent {
  @Input() title: string = '';
  @Input() value: number = 0;
  @Input() percentDifference: number = 0;
}
