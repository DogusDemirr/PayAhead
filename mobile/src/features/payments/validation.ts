import { z } from 'zod';
import { canonicalAmount } from './money';
import { frequencySchema, type PaymentWrite, type PaymentPlan } from './contracts';
import { validDate } from './dates';
import { currencyCodes } from './currencies';
export const paymentFormSchema = z.object({
  name: z.string().trim().min(1, 'Ödeme adı yaz.').max(150, 'En fazla 150 karakter kullan.'),
  description: z.string().trim().max(2000, 'En fazla 2000 karakter kullan.'),
  amount: z.string().refine((value) => canonicalAmount(value) !== null, 'Pozitif, en fazla 15 tam ve 4 ondalık basamaklı tutar yaz. Örn. 899,90'),
  currencyCode: z.string().trim().toUpperCase().refine((code) => currencyCodes.includes(code), 'Geçerli bir ISO 4217 para birimi seç.'),
  category: z.string().trim().min(1, 'Kategori yaz.').max(50, 'En fazla 50 karakter kullan.'),
  frequency: frequencySchema,
  interval: z.string().regex(/^\d+$/, 'Pozitif tam sayı yaz.').refine((value) => Number(value) >= 1 && Number(value) <= 2147483647, 'Aralık 1–2147483647 arasında olmalı.'),
  startDate: z.string().refine((value) => validDate(value) && value !== '0001-01-01', 'Geçerli tarihi YYYY-AA-GG biçiminde yaz.'),
  endDate: z.string().refine((value) => !value || validDate(value), 'Geçerli tarihi YYYY-AA-GG biçiminde yaz.'),
  anchorDay: z.string(), isActive: z.boolean(),
}).superRefine((value, context) => {
  if (value.endDate && validDate(value.endDate) && value.endDate < value.startDate) context.addIssue({ code: 'custom', path: ['endDate'], message: 'Bitiş başlangıçtan önce olamaz.' });
  if (['Monthly', 'Yearly'].includes(value.frequency) && value.anchorDay && (!/^\d+$/.test(value.anchorDay) || Number(value.anchorDay) < 1 || Number(value.anchorDay) > 31)) context.addIssue({ code: 'custom', path: ['anchorDay'], message: 'Gün 1–31 arasında olmalı.' });
});
export type PaymentFields = z.infer<typeof paymentFormSchema>;
export function formDefaults(today: string, plan?: PaymentPlan): PaymentFields {
  return plan ? { ...plan, description: plan.description ?? '', amount: plan.amount.replace('.', ','), interval: String(plan.interval), endDate: plan.endDate ?? '', anchorDay: plan.anchorDay === null ? '' : String(plan.anchorDay) } : { name: '', description: '', amount: '', currencyCode: 'TRY', category: '', frequency: 'Monthly', interval: '1', startDate: today, endDate: '', anchorDay: '', isActive: true };
}
export function toPaymentWrite(fields: PaymentFields): PaymentWrite {
  const value = paymentFormSchema.parse(fields);
  return { name: value.name, description: value.description || null, amount: canonicalAmount(value.amount)!, currencyCode: value.currencyCode, category: value.category, frequency: value.frequency, interval: Number(value.interval), startDate: value.startDate, endDate: value.endDate || null, anchorDay: ['Monthly', 'Yearly'].includes(value.frequency) && value.anchorDay ? Number(value.anchorDay) : null, isActive: value.isActive };
}
export const frequencyLabels = { Once: 'Tek seferlik', Daily: 'Günlük', Weekly: 'Haftalık', Monthly: 'Aylık', Yearly: 'Yıllık' } as const;
