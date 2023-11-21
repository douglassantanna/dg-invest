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
        <h4 class="card-title">{{ title }}</h4>
        <div class="d-flex justify-content-between align-items-center">
          <ng-container *ngIf="title !== 'Balance'; else balance">
            <span class="price-text" style="font-size: 30px; color: purple;">{{ value | currency:'USD':'symbol':'1.2-2'}}</span>
          </ng-container>
          <ng-template #balance>
            <span class="price-text" style="font-size: 30px; color: purple;">{{ value }}</span>
          </ng-template>
          <span *ngIf="percentDifference" class="percent-difference bg-success text-white px-2">{{ percentDifference | decimalRound }}</span>
        </div>
      </div>
    </div>
  `,
  styles: [``]
})
export class MyCryptoComponent {
  @Input() title: string = '';
  @Input() value: string | number = '';
  @Input() percentDifference: number = 0;
}
