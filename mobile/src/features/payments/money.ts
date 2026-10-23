// Keep backend decimal(19,4) lexical values out of IEEE-754 arithmetic.
export function canonicalAmount(value: string) {
  const input = value.trim().replace(',', '.');
  if (!/^\d+(\.\d{1,4})?$/.test(input)) return null;
  const [raw, fraction = ''] = input.split('.'); const integer = raw.replace(/^0+(?=\d)/, '');
  if (integer.length > 15 || !/[1-9]/.test(integer + fraction)) return null;
  return integer + (fraction ? `.${fraction}` : '');
}
export function financialMoney(value: string, currency: string) {
  const negative = value.startsWith('-'); const [integer, raw = ''] = value.replace(/^-/, '').split('.');
  const fraction = raw.replace(/0+$/, '').padEnd(2, '0');
  const parts = new Intl.NumberFormat('tr-TR', { style: 'currency', currency, minimumFractionDigits: 2 }).formatToParts(negative ? -1 : 1);
  return parts.map((part) => part.type === 'integer' ? integer.replace(/\B(?=(\d{3})+(?!\d))/g, '.') : part.type === 'fraction' ? fraction : part.value).join('');
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
    if (raw[after] !== ':' || !['"amount"', '"actualAmount"', '"plannedAmount"', '"paidAmount"', '"pendingAmount"', '"overdueAmount"'].includes(token)) continue;
    after++; while (/\s/.test(raw[after] ?? '') && after < raw.length) after++;
    const number = raw.slice(after).match(/^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/);
    if (number) { result += raw.slice(cursor, after) + JSON.stringify(number[0]); cursor = after + number[0].length; index = cursor - 1; }
  }
  return JSON.parse(result + raw.slice(cursor));
}
