import { t } from "../../i18n/runtime";
import { z } from 'zod';
import { canonicalAmount } from './money';
import { frequencySchema, type PaymentWrite, type PaymentPlan } from './contracts';
import { validDate } from './dates';
import { currencyCodes } from './currencies';
export const paymentFormSchema = z.object({
    catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional(),
    name: z.string().trim().min(1, { error: () => t("validation:m_51403c693698") }).max(150, { error: () => t("validation:m_9cce51c167cb") }),
    description: z.string().trim().max(2000, { error: () => t("validation:m_b2a241f9b3d3") }),
    amount: z.string().refine((value) => canonicalAmount(value) !== null, { error: () => t("validation:m_d99126b70aa4") }),
    currencyCode: z.string().trim().toUpperCase().refine((code) => currencyCodes.includes(code), { error: () => t("validation:m_ceafeb1d890f") }),
    category: z.string().trim().min(1, { error: () => t("validation:m_0406076bbb52") }).max(50, { error: () => t("validation:m_2b89bd250950") }),
    frequency: frequencySchema,
    interval: z.string().regex(/^\d+$/, { error: () => t("validation:m_9e4a7f1d9944") }).refine((value) => Number(value) >= 1 && Number(value) <= 2147483647, { error: () => t("validation:m_bbd78b58d296") }),
    startDate: z.string().refine((value) => validDate(value) && value !== '0001-01-01', { error: () => t("validation:m_d8a8544c7998") }),
    endDate: z.string().refine((value) => !value || validDate(value), { error: () => t("validation:m_d8a8544c7998") }),
    anchorDay: z.string(), isActive: z.boolean(),
}).superRefine((value, context) => {
    if (value.endDate && validDate(value.endDate) && value.endDate < value.startDate)
        context.addIssue({ code: 'custom', path: ['endDate'], message: t("validation:m_525f19e832a6") });
    if (['Monthly', 'Yearly'].includes(value.frequency) && value.anchorDay && (!/^\d+$/.test(value.anchorDay) || Number(value.anchorDay) < 1 || Number(value.anchorDay) > 31))
        context.addIssue({ code: 'custom', path: ['anchorDay'], message: t("validation:m_a9dc55ee7ab6") });
});
export type PaymentFields = z.infer<typeof paymentFormSchema>;
export function formDefaults(today: string, plan?: PaymentPlan): PaymentFields {
    return plan ? { ...plan, description: plan.description ?? '', amount: plan.amount.replace('.', ','), interval: String(plan.interval), endDate: plan.endDate ?? '', anchorDay: plan.anchorDay === null ? '' : String(plan.anchorDay) } : { catalogCategoryId: undefined, catalogServiceId: undefined, name: '', description: '', amount: '', currencyCode: 'TRY', category: '', frequency: 'Monthly', interval: '1', startDate: today, endDate: '', anchorDay: '', isActive: true };
}
export function toPaymentWrite(fields: PaymentFields): PaymentWrite {
    const value = paymentFormSchema.parse(fields);
    return { name: value.name, description: value.description || null, amount: canonicalAmount(value.amount)!, currencyCode: value.currencyCode, category: value.category, frequency: value.frequency, interval: Number(value.interval), startDate: value.startDate, endDate: value.endDate || null, anchorDay: ['Monthly', 'Yearly'].includes(value.frequency) && value.anchorDay ? Number(value.anchorDay) : null, isActive: value.isActive, ...(value.catalogCategoryId !== undefined ? { catalogCategoryId: value.catalogCategoryId, catalogServiceId: value.catalogServiceId ?? null } : {}) };
}
export const frequencyLabels = { get Once() {
        return t("validation:m_e34221564fe3");
    }, get Daily() {
        return t("validation:m_301ef766a15b");
    }, get Weekly() {
        return t("validation:m_abc3e615d675");
    }, get Monthly() {
        return t("validation:m_e055e06d7581");
    }, get Yearly() {
        return t("validation:m_9f2134dbbdd8");
    } } as const;
