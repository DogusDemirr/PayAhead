import type { PaymentFrequency } from '../../types/api';
export function leapYear(year: number) { return year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0); }
export function daysInMonth(year: number, month: number) { return [31, leapYear(year) ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1]; }
export const dayKey = (year: number, month: number, day: number) => `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
export function validDate(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split('-').map(Number);
  return year >= 1 && year <= 9999 && month >= 1 && month <= 12 && day >= 1 && day <= daysInMonth(year, month);
}
function utcDay(value: string) { const date = new Date(0); const [y, m, d] = value.split('-').map(Number); date.setUTCFullYear(y, m - 1, d); date.setUTCHours(0, 0, 0, 0); return date.getTime() / 86400000; }
export function shiftDay(value: string, days: number) { return new Date((utcDay(value) + days) * 86400000).toISOString().slice(0, 10); }
export function monthRange(today: string) { const [year, month] = today.split('-').map(Number); return { fromDate: dayKey(year, month, 1), toDate: dayKey(year, month, daysInMonth(year, month)) }; }
export function nextPlanDate(plan: { startDate: string; endDate: string | null; anchorDay: number | null; interval: number; frequency: PaymentFrequency; isActive: boolean }, today: string): string | null {
  if (!plan.isActive) return null;
  const lower = today < plan.startDate ? plan.startDate : today; const upper = plan.endDate ?? '9999-12-31';
  if (lower > upper) return null; if (lower <= plan.startDate) return plan.startDate; if (plan.frequency === 'Once') return null;
  const [year, month, day] = plan.startDate.split('-').map(Number); const [ly, lm] = lower.split('-').map(Number);
  const step = plan.interval * (plan.frequency === 'Weekly' ? 7 : 1);
  if (plan.frequency === 'Daily' || plan.frequency === 'Weekly') {
    const next = utcDay(plan.startDate) + Math.ceil((utcDay(lower) - utcDay(plan.startDate)) / step) * step;
    if (next > utcDay(upper)) return null; return new Date(next * 86400000).toISOString().slice(0, 10);
  }
  const monthly = plan.frequency === 'Monthly';
  let index = Math.max(1, Math.floor((monthly ? (ly - year) * 12 + lm - month : ly - year) / plan.interval));
  function at(i: number) {
    const total = year * 12 + month - 1 + (monthly ? i * plan.interval : 0);
    const y = monthly ? Math.floor(total / 12) : year + i * plan.interval; const m = monthly ? total % 12 + 1 : month;
    if (y > 9999) return null;
    return dayKey(y, m, Math.min(plan.anchorDay ?? day, daysInMonth(y, m)));
  }
  let next = at(index); if (next && next < lower) next = at(++index);
  return next && next <= upper ? next : null;
}
