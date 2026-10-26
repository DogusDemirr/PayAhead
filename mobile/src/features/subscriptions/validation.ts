import { t } from "../../i18n/runtime";
import { z } from 'zod';
import { canonicalAmount } from '../payments/money';
import { currencyCodes } from '../payments/currencies';
import { validDate } from '../payments/dates';
import type { Subscription, SubscriptionWrite } from './contracts';
const requiredDate = z.string().refine(x => validDate(x) && x !== '0001-01-01', { error: () => t("validation:m_8790a538a1c4") });
const optionalDate = z.string().refine(x => !x || validDate(x), { error: () => t("validation:m_8790a538a1c4") });
export const billingLabels = { get Daily() {
        return t("validation:m_301ef766a15b");
    }, get Weekly() {
        return t("validation:m_abc3e615d675");
    }, get Monthly() {
        return t("validation:m_e055e06d7581");
    }, get Yearly() {
        return t("validation:m_9f2134dbbdd8");
    } } as const;
export const subscriptionFormSchema = z.object({
    catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional(),
    name: z.string().trim().min(1, { error: () => t("validation:m_53fb989d42a5") }).max(150), providerName: z.string().trim().min(1, { error: () => t("validation:m_f2b4cfecfac2") }).max(100), description: z.string().trim().max(2000),
    amount: z.string().refine(x => canonicalAmount(x) !== null, { error: () => t("validation:m_652316e16657") }), currencyCode: z.string().trim().toUpperCase().refine(x => currencyCodes.includes(x), { error: () => t("validation:m_9ca92bf2de8d") }),
    billingFrequency: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly']), billingInterval: z.string().regex(/^\d+$/, { error: () => t("validation:m_3603d7f2ee2f") }).refine(x => Number(x) >= 1 && Number(x) <= 2147483647, { error: () => t("validation:m_0b006dd3e844") }),
    startDate: requiredDate, firstBillingDate: requiredDate, trialStartDate: optionalDate, trialEndDate: optionalDate, accessEndsOn: optionalDate,
    anchorDay: z.string(), status: z.enum(['Active', 'Trial']), autoRenew: z.boolean(), enableTrialReminders: z.boolean(),
}).superRefine((v, c) => {
    const issue = (path: string, message: string) => c.addIssue({ code: 'custom', path: [path], message });
    if (v.firstBillingDate < v.startDate)
        issue('firstBillingDate', t("validation:m_579d1caa7d8c"));
    if (!!v.trialStartDate !== !!v.trialEndDate || v.status === 'Trial' && !v.trialStartDate)
        issue('trialStartDate', t("validation:m_30f875728cf9"));
    if (v.trialStartDate && v.trialEndDate) {
        if (v.trialStartDate < v.startDate)
            issue('trialStartDate', t("validation:m_bf89ccd17e64"));
        if (v.trialEndDate < v.trialStartDate)
            issue('trialEndDate', t("validation:m_c3e70fe9969f"));
        if (v.firstBillingDate <= v.trialEndDate)
            issue('firstBillingDate', t("validation:m_5b7d76f73e12"));
    }
    if (v.anchorDay && (!/^\d+$/.test(v.anchorDay) || Number(v.anchorDay) < 1 || Number(v.anchorDay) > 31))
        issue('anchorDay', t("validation:m_41165b001f79"));
    if (!v.autoRenew && v.status === 'Active' && !v.accessEndsOn)
        issue('accessEndsOn', t("validation:m_d4aa12f6efd2"));
    if (v.accessEndsOn && (v.accessEndsOn < v.startDate || v.autoRenew && v.accessEndsOn < v.firstBillingDate))
        issue('accessEndsOn', t("validation:m_d649c199ee3b"));
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
    if (v.accessEndsOn && v.accessEndsOn < today)
        return false;
    return !(v.status === 'Trial' && v.trialEndDate && v.trialEndDate < today && !v.autoRenew);
}
