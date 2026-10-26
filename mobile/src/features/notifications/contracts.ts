import { languages } from '../../i18n/languages';
import { t } from "../../i18n/runtime";
import { z } from 'zod';
import { currencyCodes } from '../payments/currencies';
export const reminderSchema = z.object({ id: z.uuid(), paymentPlanId: z.uuid(), daysBefore: z.number().int().min(0).max(365), timeOfDay: z.string().regex(/^([01]\d|2[0-3]):[0-5]\d$/), isEnabled: z.boolean() });
export type Reminder = z.infer<typeof reminderSchema>;
export type ReminderWrite = Pick<Reminder, 'daysBefore' | 'timeOfDay' | 'isEnabled'>;
export const reminderFormSchema = z.object({ daysBefore: z.string().regex(/^\d+$/, { error: () => t("validation:m_c7f4e5354a2e") }).refine(x => Number(x) <= 365, { error: () => t("validation:m_812b82ac9973") }), timeOfDay: z.string().regex(/^([01]\d|2[0-3]):[0-5]\d$/, { error: () => t("validation:m_5a2a329dd50e") }), isEnabled: z.boolean() });
export type ReminderFields = z.infer<typeof reminderFormSchema>;
export const deviceSchema = z.object({ id: z.uuid(), platform: z.enum(['android', 'ios']), isActive: z.boolean(), lastSeenAtUtc: z.string() });
export type RegisteredDevice = z.infer<typeof deviceSchema>;
export const settingsSchema = z.object({ languageCode: z.enum(languages.map(l => l.code)), defaultCurrencyCode: z.string(), timeZoneId: z.string(), notificationsEnabled: z.boolean(), usedTimeZoneFallback: z.boolean() });
export type NotificationSettings = z.infer<typeof settingsSchema>;
export type SettingsWrite = Omit<NotificationSettings, 'usedTimeZoneFallback'>;
export function validTimeZone(value: string) {
    try {
        if (!value || value.length > 100)
            return false;
        new Intl.DateTimeFormat('tr', { timeZone: value }).format();
        return true;
    }
    catch {
        return false;
    }
}
export const settingsFormSchema = z.object({ languageCode: z.enum(languages.map(l => l.code)), defaultCurrencyCode: z.string().trim().toUpperCase().refine(x => currencyCodes.includes(x), { error: () => t("validation:m_9ca92bf2de8d") }), timeZoneId: z.string().trim().refine(validTimeZone, { error: () => t("validation:m_c8e8f2254b43") }), notificationsEnabled: z.boolean() });
export const notificationTargetSchema = z.object({ notificationId: z.uuid(), kind: z.enum(['payment', 'subscription']), resourceId: z.uuid() });
export type NotificationTarget = z.infer<typeof notificationTargetSchema>;
