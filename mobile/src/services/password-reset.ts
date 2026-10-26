import { t } from "../i18n/runtime";
import { z } from 'zod';
import { createApiClient } from './api';
const client = createApiClient(process.env.EXPO_PUBLIC_API_BASE_URL);
export const forgotSchema = z.object({ email: z.email({ error: () => t("common:m_632f0add5824") }).max(254) });
export const resetSchema = z.object({ token: z.string().regex(/^[A-Za-z0-9_-]{43}$/, { error: () => t("common:m_d3cd4aca0609") }), password: z.string().min(12, { error: () => t("common:m_bf6b1946fd11") }).max(128), confirmation: z.string() }).refine(x => x.password === x.confirmation, { path: ['confirmation'], get message() {
        return t("common:m_782480e8798a");
    } });
export function extractResetToken(input: string) {
    const value = input.trim();
    if (/^[A-Za-z0-9_-]{43}$/.test(value))
        return value;
    try {
        const token = new URL(value).searchParams.get('token');
        return token && /^[A-Za-z0-9_-]{43}$/.test(token) ? token : '';
    }
    catch {
        return '';
    }
}
export const passwordResetApi = {
    async forgot(email: string) { await client.post('/api/auth/forgot-password', { email }); },
    async reset(token: string, password: string) { await client.post('/api/auth/reset-password', { token, password }); },
};
