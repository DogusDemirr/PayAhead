import { afterEach, expect, test } from '@jest/globals';
import { financialMoney, parseFinancialJson } from '../src/features/payments/money';
import { i18next, useLanguageStore } from '../src/i18n/runtime';
import { languages } from '../src/i18n/languages';

function select(code: typeof languages[number]['code']) { void i18next.changeLanguage(code); useLanguageStore.setState({ language: code }); }
afterEach(() => select('tr'));

test('Android Intl test constraint rejects BigInt rather than silently accepting it in Node', () => {
  expect(() => new Intl.NumberFormat('en').format(BigInt('1'))).toThrow('Cannot convert bigint to number');
  expect(() => new Intl.NumberFormat('en').formatToParts(BigInt('1'))).toThrow('Cannot convert bigint to number');
});
test('large exact decimals render without BigInt or whole-amount Number coercion', () => {
  select('en');
  expect(financialMoney('999999999999999.9999', 'USD')).toBe('$999,999,999,999,999.9999');
  // Totals may exceed the safe integer range even though individual decimal(19,4) rows do not.
  expect(financialMoney('9007199254740993.0001', 'USD')).toBe('$9,007,199,254,740,993.0001');
  expect(financialMoney('-0.0001', 'USD')).toBe('-$0.0001');
  expect(financialMoney('0000123.4500', 'USD')).toBe('$123.45');
});
test.each(languages)('$code keeps locale and currency formatting with number-only Intl', ({ code, locale }) => {
  select(code);
  for (const currency of ['TRY', 'USD', 'EUR', 'JPY', 'KWD']) {
    // These small dyadic values are exactly representable and provide an independent Intl oracle.
    for (const value of [0, 1, 1000, 10000, 1234567.125, -1234567.125]) {
      expect(financialMoney(String(value), currency)).toBe(new Intl.NumberFormat(locale, { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(value));
    }
  }
});
test('API decimal lexemes and large UUID identifiers are unchanged by mapping and formatting', () => {
  const id = 'ffffffff-ffff-4fff-bfff-ffffffffffff';
  const categoryId = 'eeeeeeee-eeee-4eee-beee-eeeeeeeeeeee';
  const serviceId = 'dddddddd-dddd-4ddd-bddd-dddddddddddd';
  const raw = `{"id":"${id}","catalogCategoryId":"${categoryId}","catalogServiceId":"${serviceId}","amount":999999999999999.9999}`;
  const mapped = parseFinancialJson(raw);
  expect(mapped).toEqual({ id, catalogCategoryId: categoryId, catalogServiceId: serviceId, amount: '999999999999999.9999' });
  expect(() => financialMoney('999999999999999.9999', 'TRY')).not.toThrow();
  expect(mapped).toEqual({ id, catalogCategoryId: categoryId, catalogServiceId: serviceId, amount: '999999999999999.9999' });
});
