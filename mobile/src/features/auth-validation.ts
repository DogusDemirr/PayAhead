import { t } from "../i18n/runtime";
import { z } from 'zod';
const email = z.string().trim().max(254, { error: () => t("validation:m_e41e9e9f2fdb") }).pipe(z.email({ error: () => t("validation:m_4a6cbab3aabd") }));
export const loginSchema = z.object({ email, password: z.string().min(1, { error: () => t("validation:m_1c0653073271") }).max(128, { error: () => t("validation:m_337e59f03420") }), displayName: z.string(), confirmation: z.string().default('') });
export const registerSchema = loginSchema.extend({
    password: z.string().min(12, { error: () => t("validation:m_bf6b1946fd11") }).max(128, { error: () => t("validation:m_337e59f03420") }),
    displayName: z.string().trim().min(1, { error: () => t("validation:m_fedb94fa1ddd") }).max(100, { error: () => t("validation:m_503fe2c9852c") }),
}).refine(x => x.password === x.confirmation, { path: ['confirmation'], error: () => t('settings:profile.passwordMismatch') });
export type AuthFields = z.input<typeof loginSchema>;
export type AuthValues = z.output<typeof loginSchema>;
