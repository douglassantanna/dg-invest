// Pure helpers behind the input mask directives. Display is en-US (1,234.56); the form value is a plain number.

export interface MaskedNumber {
  display: string;
  value: number | null;
}

// Keeps digits and the first '.', limits decimals, removes leading zeros and groups thousands with ','.
// A trailing '.' is kept while typing ("12." shows "12." and the value is 12).
export function maskNumber(raw: string, maxDecimals: number): MaskedNumber {
  const cleaned = raw.replace(/[^\d.]/g, '');
  const dot = cleaned.indexOf('.');
  let integer = dot === -1 ? cleaned : cleaned.slice(0, dot);
  const fraction = dot === -1 || maxDecimals <= 0
    ? null
    : cleaned.slice(dot + 1).replace(/\./g, '').slice(0, maxDecimals);

  integer = integer.replace(/^0+(?=\d)/, '');
  if (integer === '' && fraction !== null)
    integer = '0';

  const grouped = integer.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  const display = fraction === null ? grouped : `${grouped}.${fraction}`;
  const value = integer === '' ? null : Number(fraction ? `${integer}.${fraction}` : integer);
  return { display, value };
}

// Display for a value set by the form (reset, patch). Not rounded: only the decimals beyond maxDecimals are cut.
export function formatNumber(value: unknown, maxDecimals: number): string {
  if (value === null || value === undefined || value === '')
    return '';
  const numeric = typeof value === 'number' ? value : Number(value);
  if (!Number.isFinite(numeric))
    return '';
  const plain = numeric.toLocaleString('en-US', { useGrouping: false, maximumFractionDigits: 20 });
  return maskNumber(plain, maxDecimals).display;
}

export function digitsOnly(raw: string): string {
  return raw.replace(/\D/g, '');
}

// Cursor position in the new display that keeps the same number of significant characters (digits and '.') before it.
export function caretAfterSignificant(display: string, significantBefore: number): number {
  if (significantBefore <= 0)
    return 0;
  let seen = 0;
  for (let index = 0; index < display.length; index++) {
    if (/[\d.]/.test(display[index]))
      seen++;
    if (seen === significantBefore)
      return index + 1;
  }
  return display.length;
}

export function countSignificant(text: string): number {
  return (text.match(/[\d.]/g) ?? []).length;
}
