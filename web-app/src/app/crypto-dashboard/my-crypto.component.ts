import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { DecimalRoundPipe } from '../pipes/decimal-round.pipe';

@Component({
  selector: 'app-my-crypto',
  standalone: true,
  imports: [
    CommonModule,
    DecimalRoundPipe],
  template: `
    <div class="card bg-light text-dark border border-1 rounded">
      <div class="card-body">
        <h4 class="card-title text-nowrap">{{ title }}</h4>
        <div class="d-flex justify-content-between align-items-center">
          <ng-container *ngIf="title !== 'Balance'; else balance">
            <span class="price-text" style="font-size: 22px; color: purple;">{{ value | currency:'USD':'symbol':'1.2-2'}}</span>
          </ng-container>
          <ng-template #balance>
            <span class="price-text" style="font-size: 22px; color: purple;">{{ value }}</span>
          </ng-template>
          <span
            *ngIf="percentDifference"
            class="px-2"
            [attr.data-status]="percentDifference < 0 ? 'negative' : 'positive'">
            {{ percentDifference | decimalRound }}%
          </span>
        </div>
      </div>
    </div>
  `,
  styles: [`
  span {
    color: white;
  }
  span[data-status="negative"] {
  background-color: red;
  }
  span[data-status="positive"] {
    background-color: green;
  }
  `]
})
export class MyCryptoComponent {
  @Input() title: string = '';
  @Input() value: string | number = '';
  @Input() percentDifference: number | null = 0;
}
