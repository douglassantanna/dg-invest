import { Directive, ElementRef, forwardRef, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { digitsOnly } from './input-masks';

// Text input that accepts digits only (IDs such as a Bybit UID). The value stays a string.
@Directive({
  selector: 'input[appDigitsOnly]',
  standalone: true,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => DigitsOnlyDirective), multi: true }],
  host: {
    inputmode: 'numeric',
    '(input)': 'onInput()',
    '(blur)': 'onTouched()'
  }
})
export class DigitsOnlyDirective implements ControlValueAccessor {
  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef).nativeElement;
  private onChange: (value: string) => void = () => { };
  onTouched: () => void = () => { };

  writeValue(value: unknown): void {
    this.input.value = value === null || value === undefined ? '' : digitsOnly(String(value));
  }

  registerOnChange(fn: (value: string) => void): void {
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
    const digitsBefore = digitsOnly(this.input.value.slice(0, caret)).length;
    const cleaned = digitsOnly(this.input.value);
    this.input.value = cleaned;
    this.input.setSelectionRange(digitsBefore, digitsBefore);
    this.onChange(cleaned);
  }
}
