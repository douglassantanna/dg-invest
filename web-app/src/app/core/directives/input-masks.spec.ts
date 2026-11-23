import { caretAfterSignificant, digitsOnly, formatNumber, maskNumber } from './input-masks';

describe('input masks', () => {
  describe('maskNumber', () => {
    it('groups thousands and returns a plain number', () => {
      expect(maskNumber('1234567.89', 2)).toEqual({ display: '1,234,567.89', value: 1234567.89 });
    });

    it('drops letters, extra dots and pasted separators', () => {
      expect(maskNumber('1a,2b34.5.6', 2)).toEqual({ display: '1,234.56', value: 1234.56 });
    });

    it('limits decimals without rounding', () => {
      expect(maskNumber('0.123456789', 8)).toEqual({ display: '0.12345678', value: 0.12345678 });
      expect(maskNumber('10.999', 2)).toEqual({ display: '10.99', value: 10.99 });
    });

    it('keeps a trailing dot while typing', () => {
      expect(maskNumber('12.', 2)).toEqual({ display: '12.', value: 12 });
    });

    it('turns a leading dot into 0.', () => {
      expect(maskNumber('.5', 8)).toEqual({ display: '0.5', value: 0.5 });
    });

    it('removes leading zeros but keeps a single 0', () => {
      expect(maskNumber('000123', 2)).toEqual({ display: '123', value: 123 });
      expect(maskNumber('0', 2)).toEqual({ display: '0', value: 0 });
    });

    it('returns null for empty input', () => {
      expect(maskNumber('', 2)).toEqual({ display: '', value: null });
      expect(maskNumber('abc', 2)).toEqual({ display: '', value: null });
    });
  });

  describe('formatNumber', () => {
    it('formats values set by the form', () => {
      expect(formatNumber(1234.5, 2)).toBe('1,234.5');
      expect(formatNumber(0.00000012, 8)).toBe('0.00000012');
    });

    it('shows empty for empty values', () => {
      expect(formatNumber('', 2)).toBe('');
      expect(formatNumber(null, 2)).toBe('');
    });
  });

  it('digitsOnly keeps only digits', () => {
    expect(digitsOnly(' 100-01a ')).toBe('10001');
  });

  it('caretAfterSignificant skips the inserted separators', () => {
    // "1234|" -> "1,234|": 4 significant characters before the cursor.
    expect(caretAfterSignificant('1,234', 4)).toBe(5);
    // "12|34" -> "1,2|34"
    expect(caretAfterSignificant('1,234', 2)).toBe(3);
  });
});
