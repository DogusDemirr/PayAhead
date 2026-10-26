import { t } from "../../i18n/runtime";
import type { AxiosInstance } from 'axios';
import { CanceledError } from 'axios';
import { z } from 'zod';
import { ApiError } from '../../services/api-error';
import type { FinanceContext } from '../payments/api';
import { canonicalAmount, parseFinancialJson } from '../payments/money';
import { subscriptionSchema, subscriptionPageSchema, subscriptionSummarySchema, trialSchema, type SubscriptionWrite, type SubscriptionFilter } from './contracts';
export function serializeSubscription(body: SubscriptionWrite, rowVersion?: string) {
    const { amount: input, ...fields } = body;
    const amount = canonicalAmount(input);
    if (!amount)
        throw new ApiError(t("subscriptions:m_8cb49b7388e8"));
    const json = JSON.stringify({ ...fields, ...(rowVersion ? { rowVersion } : {}) });
    return json.slice(0, -1) + `,"amount":${amount}}`;
}
export function createSubscriptionApi(client: AxiosInstance, context: FinanceContext = {}) {
    function check() { if (context.isCurrent && !context.isCurrent())
        throw new CanceledError(t("subscriptions:m_72ec2727147f")); }
    function config(signal?: AbortSignal) { check(); return { signal, sessionEpoch: context.epoch, transformResponse: [parseFinancialJson] }; }
    function read<T>(schema: z.ZodType<T>, data: unknown) { check(); const result = schema.safeParse(data); if (!result.success)
        throw new ApiError(t("subscriptions:m_4fcd73a757cc")); return result.data; }
    async function write(method: 'post' | 'put' | 'delete', path: string, data?: string, rowVersion?: string) {
        check();
        await context.prepareWrite?.();
        check();
        const response = await client.request({ ...config(), method, url: path, data, headers: { 'Content-Type': 'application/json', ...(method === 'delete' ? { 'If-Match': `"${rowVersion}"` } : {}) } });
        check();
        return method === 'delete' ? undefined : read(subscriptionSchema, response.data);
    }
    const path = (id: string) => `/api/subscriptions/${encodeURIComponent(id)}`;
    return {
        list: async (filter: SubscriptionFilter, signal?: AbortSignal) => read(subscriptionPageSchema, (await client.get('/api/subscriptions', { ...config(signal), params: filter })).data),
        detail: async (id: string, signal?: AbortSignal) => read(subscriptionSchema, (await client.get(path(id), config(signal))).data),
        summary: async (signal?: AbortSignal) => read(subscriptionSummarySchema, (await client.get('/api/subscriptions/summary', config(signal))).data),
        trials: async (withinDays = 7, signal?: AbortSignal) => read(z.array(trialSchema), (await client.get('/api/subscriptions/trials/expiring', { ...config(signal), params: { withinDays } })).data),
        create: (body: SubscriptionWrite) => write('post', '/api/subscriptions', serializeSubscription(body)),
        update: (id: string, body: SubscriptionWrite, version: string) => write('put', path(id), serializeSubscription(body, version)),
        cancel: (id: string, accessEndsOn: string, rowVersion: string) => write('post', path(id) + '/cancel', JSON.stringify({ accessEndsOn, rowVersion })),
        remove: (id: string, rowVersion: string) => write('delete', path(id), undefined, rowVersion),
    };
}
