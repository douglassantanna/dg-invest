import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'formatCurrency',
  standalone: true
})
export class FormatCurrencyPipe implements PipeTransform {
  transform(amount: number): string {
    let options: Intl.NumberFormatOptions;
    if (amount < 1) {
      options = { style: 'currency', currency: 'USD', minimumFractionDigits: 4, maximumFractionDigits: 4 };
    } else {
      options = { style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2 };
    }
    return new Intl.NumberFormat('en-US', options).format(amount);
  }
}
