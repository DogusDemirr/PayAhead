import { currentLocale } from '../../i18n/runtime';
// Keep backend decimal(19,4) lexical values out of IEEE-754 arithmetic.
export function canonicalAmount(value: string) {
  const input = value.trim().replace(/[٠-٩۰-۹०-९０-９]/g, digit => {
    const point = digit.codePointAt(0)!;
    const start = point >= 0xff10 ? 0xff10 : point >= 0x0966 ? 0x0966 : point >= 0x06f0 ? 0x06f0 : 0x0660;
    return String(point - start);
  }).replace('٫', '.').replace(',', '.');
  if (!/^\d+(\.\d{1,4})?$/.test(input)) return null;
  const [raw, fraction = ''] = input.split('.'); const integer = raw.replace(/^0+(?=\d)/, '');
  if (integer.length > 15 || !/[1-9]/.test(integer + fraction)) return null;
  return integer + (fraction ? `.${fraction}` : '');
}
export function financialMoney(value: string, currency: string) {
  const negative = value.startsWith('-'); const [integer, raw = ''] = value.replace(/^-/, '').split('.');
  const fraction = raw.replace(/0+$/, '').padEnd(2, '0');
  const locale = currentLocale();
  const parts = new Intl.NumberFormat(locale, { style: 'currency', currency, minimumFractionDigits: 2 }).formatToParts(negative ? -1 : 1);
  const rules = integerRules(locale);
  const localize = (text: string) => text.replace(/\d/g, digit => rules.digits[digit.charCodeAt(0) - 48]);
  const normalized = integer.replace(/^0+(?=\d)/, '');
  const groups: string[] = [];
  if (rules.separator && normalized.length >= rules.minimumLength) {
    let end = normalized.length, size = rules.primary;
    while (end > 0) {
      const start = Math.max(0, end - size);
      groups.unshift(localize(normalized.slice(start, end)));
      end = start; size = rules.secondary;
    }
  } else groups.push(localize(normalized));
  return parts.map(part => part.type === 'integer' ? groups.join(rules.separator) : part.type === 'fraction' ? localize(fraction) : part.value).join('');
}

type IntegerRules = { digits: string[]; separator: string; primary: number; secondary: number; minimumLength: number };
const rulesByLocale = new Map<string, IntegerRules>();
function integerRules(locale: string): IntegerRules {
  const cached = rulesByLocale.get(locale); if (cached) return cached;
  // Android Hermes Intl may reject BigInt and coerce strings to Number. Probe only
  // small exact Numbers; group/localize the real decimal as text, including totals
  // beyond MAX_SAFE_INTEGER. Locale data controls Indian grouping and Spanish 1000.
  const formatter = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
  const probe = formatter.formatToParts(123456789);
  const integers = probe.filter(part => part.type === 'integer');
  const digits = new Intl.NumberFormat(locale, { useGrouping: false, maximumFractionDigits: 0 });
  const rules: IntegerRules = {
    digits: Array.from({ length: 10 }, (_, digit) => digits.format(digit)),
    separator: probe.find(part => part.type === 'group')?.value ?? '',
    primary: [...integers[integers.length - 1].value].length,
    secondary: [...integers[Math.max(0, integers.length - 2)].value].length,
    minimumLength: formatter.formatToParts(1000).some(part => part.type === 'group') ? 4 : 5,
  };
  rulesByLocale.set(locale, rules); return rules;
}
export function parseFinancialJson(raw: unknown): unknown {
  if (typeof raw !== 'string' || !raw) return raw;
  let result = ''; let cursor = 0;
  // Token-aware key scan: quoted text inside descriptions is never rewritten.
  for (let index = 0; index < raw.length; index++) {
    if (raw[index] !== '"') continue;
    const start = index++;
    while (index < raw.length) { if (raw[index] === '\\') { index += 2; continue; } if (raw[index] === '"') break; index++; }
    const token = raw.slice(start, index + 1); let after = index + 1;
    while (/\s/.test(raw[after] ?? '') && after < raw.length) after++;
    if (raw[after] !== ':' || !['"amount"', '"actualAmount"', '"plannedAmount"', '"paidAmount"', '"pendingAmount"', '"overdueAmount"', '"estimatedAmount"'].includes(token)) continue;
    after++; while (/\s/.test(raw[after] ?? '') && after < raw.length) after++;
    const number = raw.slice(after).match(/^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/);
    if (number) { result += raw.slice(cursor, after) + JSON.stringify(number[0]); cursor = after + number[0].length; index = cursor - 1; }
  }
  return JSON.parse(result + raw.slice(cursor));
}
