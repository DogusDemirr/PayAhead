import { currentLocale, t } from '../i18n/runtime';
export function money(amount: number, currency = 'TRY') {
    return new Intl.NumberFormat(currentLocale(), { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);
}
export function dateLabel(date: string, long = false, includeYear = false) {
    // DateOnly is a calendar day: never parse as UTC then shift it to a device timezone.
    const [year, month, day] = date.split('-').map(Number);
    const local = new Date(0);
    local.setFullYear(year, month - 1, day);
    local.setHours(12, 0, 0, 0);
    return new Intl.DateTimeFormat(currentLocale(), { day: 'numeric', month: long ? 'long' : 'short', ...(includeYear ? { year: 'numeric' as const } : {}) }).format(local);
}
export const dateKey = (year: number, month: number, day: number) => `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
export function greeting(hour: number) { return hour < 12 ? t("common:m_cd1e0724e02a") : hour < 18 ? t("common:m_badc3e7e2b20") : t("common:m_7df387187c77"); }
export function currencyTotals(items: readonly {
    amount: number;
    currencyCode: string;
}[]) {
    return Object.entries(items.reduce<Record<string, number>>((totals, item) => {
        totals[item.currencyCode] = (totals[item.currencyCode] ?? 0) + item.amount;
        return totals;
    }, {})).map(([currencyCode, amount]) => ({ currencyCode, amount }));
}
