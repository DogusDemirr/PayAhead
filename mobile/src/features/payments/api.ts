import type { AxiosInstance } from 'axios';
import { CanceledError } from 'axios';
import { z } from 'zod';
import { ApiError } from '../../services/api-error';
import { paymentSchema, pageSchema, summarySchema, upcomingSchema, entitlementSchema, calendarSchema, occurrenceSchema, analyticsSchema, type OccurrenceStatus, type PaymentWrite, type PaymentPlan, type PlanFilter } from './contracts';
import { canonicalAmount, parseFinancialJson } from './money';
export function serializePayment(body: PaymentWrite, rowVersion?: string) {
  const { amount: input, ...rest } = body;
  const amount = canonicalAmount(input); if (!amount) throw new ApiError('Tutar geçersiz.');
  const encoded = JSON.stringify({ ...rest, ...(rowVersion ? { rowVersion } : {}) });
  return encoded.slice(0, -1) + `,"amount":${amount}}`;
}
export interface FinanceContext { epoch?: number; isCurrent?: () => boolean; prepareWrite?: () => Promise<void> }
export function createFinanceApi(client: AxiosInstance, scope: FinanceContext = {}) {
  function check() { if (scope.isCurrent && !scope.isCurrent()) throw new CanceledError('Oturum değişti.'); }
  function config(signal?: AbortSignal) { check(); return { signal, sessionEpoch: scope.epoch, transformResponse: [parseFinancialJson] }; }
  function read<T>(schema: z.ZodType<T>, data: unknown): T {
    const parsed = schema.safeParse(data); if (!parsed.success) throw new ApiError('Sunucu yanıtı beklenen biçimde değil. Daha sonra tekrar dene.'); return parsed.data;
  }
  async function write(method: 'post' | 'put' | 'delete', url: string, schema: z.ZodType<PaymentPlan> | null, body?: PaymentWrite, rowVersion?: string): Promise<PaymentPlan | undefined> {
    check(); await scope.prepareWrite?.(); check();
    const response = await client.request({ ...config(), method, url, data: body ? serializePayment(body, rowVersion) : undefined, headers: { 'Content-Type': 'application/json', ...(method === 'delete' ? { 'If-Match': `"${rowVersion}"` } : {}) } });
    return schema ? read(schema, response.data) : undefined;
  }
  return {
    list: async (filter: PlanFilter, signal?: AbortSignal) => read(pageSchema(paymentSchema), (await client.get('/api/payments', { ...config(signal), params: filter })).data),
    detail: async (id: string, signal?: AbortSignal) => read(paymentSchema, (await client.get(`/api/payments/${encodeURIComponent(id)}`, config(signal))).data),
    summary: async (range?: { fromDate: string; toDate: string }, signal?: AbortSignal) => read(summarySchema, (await client.get('/api/dashboard/summary', { ...config(signal), params: range })).data),
    upcoming: async (params: { fromDate: string; toDate: string; pageNumber: number; pageSize: number; status: OccurrenceStatus }, signal?: AbortSignal) => read(upcomingSchema, (await client.get('/api/payments/upcoming', { ...config(signal), params })).data),
    calendar: async (year: number, month: number, signal?: AbortSignal) => read(calendarSchema, (await client.get('/api/calendar', { ...config(signal), params: { year, month } })).data),
    analytics: async (year: number, month: number, signal?: AbortSignal, currencyCode?: string) => read(analyticsSchema, (await client.get('/api/analytics', { ...config(signal), params: { year, month, currencyCode } })).data),
    occurrence: async (id: string, date: string, signal?: AbortSignal) => read(occurrenceSchema, (await client.get(`/api/payments/${encodeURIComponent(id)}/occurrences/${encodeURIComponent(date)}`, config(signal))).data),
    setOccurrence: async (id: string, date: string, status: OccurrenceStatus, rowVersion: string | null, input?: string) => {
      check(); await scope.prepareWrite?.(); check();
      const amount = status === 'Paid' ? canonicalAmount(input ?? '') : null;
      if (status === 'Paid' && !amount) throw new ApiError('Gerçek ödenen tutarı gir.');
      const body = JSON.stringify({ status, rowVersion });
      const data = body.slice(0, -1) + `,"actualAmount":${amount ?? 'null'}}`;
      return read(occurrenceSchema, (await client.put(`/api/payments/${encodeURIComponent(id)}/occurrences/${encodeURIComponent(date)}/status`, data, { ...config(), headers: { 'Content-Type': 'application/json' } })).data);
    },
    entitlements: async (signal?: AbortSignal) => read(entitlementSchema, (await client.get('/api/premium/entitlements', config(signal))).data),
    create: (body: PaymentWrite) => write('post', '/api/payments', paymentSchema, body),
    update: (id: string, body: PaymentWrite, rowVersion: string) => write('put', `/api/payments/${encodeURIComponent(id)}`, paymentSchema, body, rowVersion),
    remove: (id: string, rowVersion: string) => write('delete', `/api/payments/${encodeURIComponent(id)}`, null, undefined, rowVersion),
  };
}
