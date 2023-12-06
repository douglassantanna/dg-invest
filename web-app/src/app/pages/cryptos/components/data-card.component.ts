import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { DecimalRoundPipe } from '../../../core/pipes/decimal-round.pipe';
import { PercentDifferenceComponent } from './percent-difference.component';

@Component({
  selector: 'app-data-card',
  standalone: true,
  imports: [
    CommonModule,
    DecimalRoundPipe,
    PercentDifferenceComponent],
  template: `
    <div class="card bg-light text-dark border border-1 rounded mb-3">
      <div class="card-body">
        <h4 class="card-title text-nowrap">{{ title }}</h4>
        <div class="d-flex justify-content-between align-items-center">
          <ng-container *ngIf="title !== 'Balance'; else balance">
            <span class="price-text" style="font-size: 22px; color: purple;">{{ value | currency:'USD':'symbol':'1.2-2'}}</span>
          </ng-container>
          <ng-template #balance>
            <span class="price-text" style="font-size: 22px; color: purple;">{{ value }}</span>
          </ng-template>
          <app-percent-difference *ngIf="percentDifference" [percentDifference]="percentDifference" />
        </div>
      </div>
    </div>
  `,
  styles: [`
  span {
    color: white;
  }
  `]
})
export class DataCardComponent {
  @Input() title: string = '';
  @Input() value: string | number = '';
  @Input() percentDifference: number = 0;
}
