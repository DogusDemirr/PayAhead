import { t } from "../i18n/runtime";
import { isAxiosError, isCancel } from 'axios';
import type { ProblemDetails } from '../types/api';
export class ApiError extends Error {
    constructor(message: string, public readonly status?: number, public readonly traceId?: string, public readonly errors?: Record<string, string[]>, public readonly kind: 'http' | 'network' | 'timeout' | 'configuration' | 'session' = 'http', public readonly code?: string) {
        super(message);
        this.name = 'ApiError';
    }
}
export function mapApiError(error: unknown): Error {
    if (error instanceof ApiError || isCancel(error))
        return error;
    if (!isAxiosError<ProblemDetails>(error))
        return new ApiError(t("errors:m_b7ac953bddf5"));
    if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT')
        return new ApiError(t("errors:m_97fef89efdf8"), undefined, undefined, undefined, 'timeout');
    if (!error.response)
        return new ApiError(t("errors:m_148edfcba033"), undefined, undefined, undefined, 'network');
    const { status, data } = error.response;
    const registration = error.config?.url === '/api/auth/register';
    const reset = ['/api/auth/forgot-password', '/api/auth/reset-password'].includes(error.config?.url ?? '');
    if (reset && [401, 404].includes(status))
        return new ApiError(t("errors:m_64872a0e18b3"), status, data?.traceId);
    if (error.config?.url === '/api/auth/reset-password' && status === 400 && !data?.errors)
        return new ApiError(t("errors:m_96d8c326f702"), status, data?.traceId);
    const messages: Record<number, string> = { 401: t("errors:m_0d2b1db5ac23"), 403: data?.code === 'entitlement_limit' ? t("errors:m_99a158d3b327") : t("errors:m_a25c909874f9"), 404: t("errors:m_cfec70e3a8bc"), 409: registration ? t("errors:m_4caef90dffc6") : t("errors:m_73614d4af236"), 429: t("errors:m_bef85276f101") };
    return new ApiError(messages[status] ?? (status >= 500 ? t("errors:m_79cea1288206") : t("errors:m_2d8988bb1fc4")), status, data?.traceId, data?.errors ? Object.fromEntries(Object.keys(data.errors).map(field => [field, [t("validation:invalidValue")]])) : undefined, 'http', data?.code);
}
