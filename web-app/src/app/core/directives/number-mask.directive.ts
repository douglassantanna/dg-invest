import { Directive, ElementRef, Input, OnChanges, SimpleChanges, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { caretAfterSignificant, countSignificant, formatNumber, maskNumber } from './input-masks';

// Numeric input shown as en-US (1,234.56) while typing. The form control receives a plain number (null when empty),
// so request payloads are unchanged. Use on a type="text" input: type="number" cannot display separators.
//   USD amounts: appNumberMask (2 decimals)    Coin quantities: appNumberMask [maxDecimals]="8"
@Directive({
  selector: 'input[appNumberMask]',
  standalone: true,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => NumberMaskDirective), multi: true }],
  host: {
    inputmode: 'decimal',
    autocomplete: 'off',
    '(input)': 'onInput()',
    '(blur)': 'onTouched()'
  }
})
export class NumberMaskDirective implements ControlValueAccessor, OnChanges {
  @Input() maxDecimals = 2;

  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private onChange: (value: number | null) => void = () => { };
  onTouched: () => void = () => { };

  // The precision can change after typing (e.g. a deposit switched from Crypto to Fiat): re-apply the mask to what is
  // shown and update the control, so the value sent matches what the user sees. Deferred so the control is not
  // updated while Angular is still checking the template.
  ngOnChanges(changes: SimpleChanges): void {
    const change = changes['maxDecimals'];
    if (!change || change.firstChange || this.input.value === '')
      return;
    queueMicrotask(() => {
      const masked = maskNumber(this.input.value, this.maxDecimals);
      if (masked.display === this.input.value)
        return;
      this.input.value = masked.display;
      this.onChange(masked.value);
    });
  }

  writeValue(value: unknown): void {
    this.input.value = formatNumber(value, this.maxDecimals);
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.input.disabled = isDisabled;
  }

  onInput(): void {
    const caret = this.input.selectionStart ?? this.input.value.length;
    const significantBefore = countSignificant(this.input.value.slice(0, caret));
    const masked = maskNumber(this.input.value, this.maxDecimals);
    this.input.value = masked.display;
    const position = caretAfterSignificant(masked.display, significantBefore);
    this.input.setSelectionRange(position, position);
    this.onChange(masked.value);
  }
}
