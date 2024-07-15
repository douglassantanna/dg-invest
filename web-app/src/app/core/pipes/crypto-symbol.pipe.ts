import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'cryptoSymbol',
  standalone: true
})
export class CryptoSymbolPipe implements PipeTransform {
  transform(value: string): string {
    if (!value) {
      return '';
    }
    return value.replace(/usd$/i, '');
  }
}
