import { z } from 'zod';
import { canonicalAmount } from '../payments/money';
import { currencyCodes } from '../payments/currencies';
import { validDate } from '../payments/dates';
import type { Subscription, SubscriptionWrite } from './contracts';
const requiredDate = z.string().refine(x => validDate(x) && x !== '0001-01-01', 'Geçerli bir tarih seç.');
const optionalDate = z.string().refine(x => !x || validDate(x), 'Geçerli bir tarih seç.');
export const billingLabels = { Daily: 'Günlük', Weekly: 'Haftalık', Monthly: 'Aylık', Yearly: 'Yıllık' } as const;
export const subscriptionFormSchema = z.object({
  catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional(),
  name: z.string().trim().min(1, 'Abonelik adı yaz.').max(150), providerName: z.string().trim().min(1, 'Sağlayıcı adı yaz.').max(100), description: z.string().trim().max(2000),
  amount: z.string().refine(x => canonicalAmount(x) !== null, 'Pozitif, en fazla 15 tam ve 4 ondalık basamaklı tutar gir.'), currencyCode: z.string().trim().toUpperCase().refine(x => currencyCodes.includes(x), 'Geçerli ISO 4217 kodu gir.'),
  billingFrequency: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly']), billingInterval: z.string().regex(/^\d+$/, 'Pozitif tam sayı gir.').refine(x => Number(x) >= 1 && Number(x) <= 2147483647, 'Aralık 1–2147483647 olmalı.'),
  startDate: requiredDate, firstBillingDate: requiredDate, trialStartDate: optionalDate, trialEndDate: optionalDate, accessEndsOn: optionalDate,
  anchorDay: z.string(), status: z.enum(['Active', 'Trial']), autoRenew: z.boolean(), enableTrialReminders: z.boolean(),
}).superRefine((v, c) => {
  const issue = (path: string, message: string) => c.addIssue({ code: 'custom', path: [path], message });
  if (v.firstBillingDate < v.startDate) issue('firstBillingDate', 'İlk ücretli tarih başlangıçtan önce olamaz.');
  if (!!v.trialStartDate !== !!v.trialEndDate || v.status === 'Trial' && !v.trialStartDate) issue('trialStartDate', 'Deneme başlangıç ve bitiş tarihlerini birlikte seç.');
  if (v.trialStartDate && v.trialEndDate) {
    if (v.trialStartDate < v.startDate) issue('trialStartDate', 'Deneme başlangıcı abonelik başlangıcından önce olamaz.');
    if (v.trialEndDate < v.trialStartDate) issue('trialEndDate', 'Deneme bitişi başlangıcından önce olamaz.');
    if (v.firstBillingDate <= v.trialEndDate) issue('firstBillingDate', 'İlk ücretli tarih deneme bitişinden sonra olmalı.');
  }
  if (v.anchorDay && (!/^\d+$/.test(v.anchorDay) || Number(v.anchorDay) < 1 || Number(v.anchorDay) > 31)) issue('anchorDay', 'Gün 1–31 olmalı.');
  if (!v.autoRenew && v.status === 'Active' && !v.accessEndsOn) issue('accessEndsOn', 'Otomatik yenileme kapalı aktif abonelikte erişim bitişi gerekli.');
  if (v.accessEndsOn && (v.accessEndsOn < v.startDate || v.autoRenew && v.accessEndsOn < v.firstBillingDate)) issue('accessEndsOn', 'Erişim bitişi başlangıçtan; otomatik yenilemede ilk ücretli tarihten önce olamaz.');
});
export type SubscriptionFields = z.infer<typeof subscriptionFormSchema>;
export function subscriptionDefaults(today: string, record?: Subscription): SubscriptionFields {
  return record ? { catalogCategoryId: record.catalogCategoryId, catalogServiceId: record.catalogServiceId, name: record.name, providerName: record.providerName, description: record.description ?? '', amount: record.amount.replace('.', ','), currencyCode: record.currencyCode,
    billingFrequency: record.billingFrequency, billingInterval: String(record.billingInterval), anchorDay: record.anchorDay === null ? '' : String(record.anchorDay),
    startDate: record.startDate, firstBillingDate: record.firstBillingDate, trialStartDate: record.trialStartDate ?? '', trialEndDate: record.trialEndDate ?? '', accessEndsOn: record.accessEndsOn ?? '',
    status: record.status === 'Trial' ? 'Trial' : 'Active', autoRenew: record.autoRenew, enableTrialReminders: record.enableTrialReminders }
    : { catalogCategoryId: undefined, catalogServiceId: undefined, name: '', providerName: '', description: '', amount: '', currencyCode: 'TRY', billingFrequency: 'Monthly', billingInterval: '1', anchorDay: '', startDate: today, firstBillingDate: today,
      trialStartDate: '', trialEndDate: '', accessEndsOn: '', status: 'Active', autoRenew: true, enableTrialReminders: true };
}
export function toSubscriptionWrite(fields: SubscriptionFields): SubscriptionWrite {
  const v = subscriptionFormSchema.parse(fields);
  return { ...v, amount: canonicalAmount(v.amount)!, description: v.description || null, billingInterval: Number(v.billingInterval), anchorDay: ['Monthly', 'Yearly'].includes(v.billingFrequency) && v.anchorDay ? Number(v.anchorDay) : null,
    trialStartDate: v.trialStartDate || null, trialEndDate: v.trialEndDate || null, accessEndsOn: v.accessEndsOn || null };
}
export function consumesTrackingSlot(v: SubscriptionFields, today: string) {
  if (v.accessEndsOn && v.accessEndsOn < today) return false;
  return !(v.status === 'Trial' && v.trialEndDate && v.trialEndDate < today && !v.autoRenew);
}
