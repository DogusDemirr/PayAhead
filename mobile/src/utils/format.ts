export function money(amount: number, currency = 'TRY') {
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}
export function dateLabel(date: string, long = false) {
  // DateOnly is a calendar day: never parse as UTC then shift it to a device timezone.
  const [year, month, day] = date.split('-').map(Number);
  return new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: long ? 'long' : 'short' }).format(new Date(year, month - 1, day, 12));
}
export const dateKey = (year: number, month: number, day: number) => `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
export function greeting(hour: number) { return hour < 12 ? 'Günaydın' : hour < 18 ? 'İyi günler' : 'İyi akşamlar'; }
export function currencyTotals(items: readonly { amount: number; currencyCode: string }[]) {
  return Object.entries(items.reduce<Record<string, number>>((totals, item) => {
    totals[item.currencyCode] = (totals[item.currencyCode] ?? 0) + item.amount; return totals;
  }, {})).map(([currencyCode, amount]) => ({ currencyCode, amount }));
}
