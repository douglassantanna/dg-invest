import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'decimalRound',
  standalone: true
})
export class DecimalRoundPipe implements PipeTransform {
  transform(value: number, decimalPlaces: number = 2): number {
    const multiplier = Math.pow(10, decimalPlaces);
    return Math.round(value * multiplier) / multiplier;
  }
}
