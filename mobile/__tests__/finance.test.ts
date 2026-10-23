import { test, expect } from '@jest/globals';
import { create } from 'axios';
import { canonicalAmount, financialMoney, parseFinancialJson } from '../src/features/payments/money';
import { nextPlanDate, validDate } from '../src/features/payments/dates';
import { paymentFormSchema, formDefaults, toPaymentWrite } from '../src/features/payments/validation';
import { createFinanceApi, serializePayment } from '../src/features/payments/api';
import { plan, summary } from './finance-fixtures';

test('money preserves decimal(19,4) without a float round trip', () => {
  expect(canonicalAmount('999999999999999,9999')).toBe('999999999999999.9999');
  expect(canonicalAmount('1000000000000000')).toBeNull();
  expect(canonicalAmount('0,0001')).toBe('0.0001');
  for (const bad of ['0', '-2', '1,23456', '1.000,00', 'NaN', '1e3']) expect(canonicalAmount(bad)).toBeNull();
  expect(financialMoney('999999999999999.9999', 'TRY')).toContain('999.999.999.999.999,9999');
});

test('analytics totals retain exact decimal tokens including aggregated amounts beyond one plan limit', () => {
  expect(parseFinancialJson('{"plannedAmount":1234567890123456789.1234,"paidAmount":7.4321,"pendingAmount":0,"overdueAmount":0,"paidCount":1}')).toEqual({ plannedAmount: '1234567890123456789.1234', paidAmount: '7.4321', pendingAmount: '0', overdueAmount: '0', paidCount: 1 });
});
test('occurrence serialization sends exact actualAmount and only existing status contract fields', async () => {
  const client = create(); let body = '';
  client.defaults.adapter = async config => { body = config.data; return { data: JSON.stringify({ id: null, paymentPlanId: plan.id, dueDate: '2026-10-31', status: 'Paid', actualAmount: '999999999999999.9999', paidAtUtc: '2026-10-23T12:00:00Z', createdAtUtc: null, rowVersion: plan.rowVersion, isRecorded: true }), status: 200, statusText: 'OK', headers: {}, config }; };
  await createFinanceApi(client).setOccurrence(plan.id, '2026-10-31', 'Paid', null, '999999999999999,9999');
  expect(body).toContain('"actualAmount":999999999999999.9999'); expect(Object.keys(JSON.parse(body)).sort()).toEqual(['actualAmount', 'rowVersion', 'status']);
});
test('financial JSON parsing never rewrites quoted descriptions or other numeric fields', () => {
  const raw = '{"amount":999999999999999.9999,"description":"say \\"amount\\": 1", "actualAmount":null,"interval":2}';
  const corrected = raw.replace('say \\"amount\\": 1', 'say \\"amount\\": 1');
  expect(parseFinancialJson(corrected)).toEqual({ amount: '999999999999999.9999', description: 'say "amount": 1', actualAmount: null, interval: 2 });
});
test('write JSON sends a numeric decimal token and escapes text safely', () => {
  const fields = formDefaults('2026-10-23', plan); fields.name = 'A "quoted" plan'; fields.amount = '999999999999999,9999';
  const raw = serializePayment(toPaymentWrite(fields), plan.rowVersion);
  expect(raw).toContain('"amount":999999999999999.9999');
  expect(JSON.parse(raw).name).toBe('A "quoted" plan');
  expect(JSON.parse(raw).rowVersion).toBe(plan.rowVersion);
});
test('invalid dates and end-before-start are rejected; actual leap days accepted', () => {
  expect(validDate('2024-02-29')).toBe(true); expect(validDate('2025-02-29')).toBe(false); expect(validDate('2026-04-31')).toBe(false);
  const fields = formDefaults('2026-10-23', plan);
  expect(paymentFormSchema.safeParse({ ...fields, name: ' ', amount: '-1' }).success).toBe(false);
  expect(paymentFormSchema.safeParse({ ...fields, endDate: '2025-01-01' }).success).toBe(false);
  expect(paymentFormSchema.safeParse({ ...fields, currencyCode: 'ZZZ' }).success).toBe(false);
});
test('all backend frequencies and intervals validate', () => {
  for (const frequency of ['Once', 'Daily', 'Weekly', 'Monthly', 'Yearly']) expect(paymentFormSchema.safeParse({ ...formDefaults('2026-10-23', plan), frequency }).success).toBe(true);
  expect(paymentFormSchema.safeParse({ ...formDefaults('2026-10-23', plan), interval: '0' }).success).toBe(false);
  expect(toPaymentWrite({ ...formDefaults('2026-10-23', plan), frequency: 'Daily', anchorDay: '31' }).anchorDay).toBeNull();
});
test('month-end clamping does not drift in following months', () => {
  expect(nextPlanDate(plan, '2026-02-01')).toBe('2026-02-28');
  expect(nextPlanDate(plan, '2026-03-01')).toBe('2026-03-31');
  expect(nextPlanDate({ ...plan, interval: 2 }, '2026-02-01')).toBe('2026-03-31');
});
test('Feb 29 yearly recurrence recovers leap day and respects inclusive end', () => {
  const annual = { ...plan, frequency: 'Yearly' as const, startDate: '2024-02-29', anchorDay: null };
  expect(nextPlanDate(annual, '2025-02-01')).toBe('2025-02-28');
  expect(nextPlanDate(annual, '2028-02-01')).toBe('2028-02-29');
  expect(nextPlanDate({ ...annual, endDate: '2025-02-27' }, '2025-02-01')).toBeNull();
});
test('startDate stays first even with another anchor; once/past/inactive are explicit', () => {
  expect(nextPlanDate({ ...plan, startDate: '2026-10-01', anchorDay: 31 }, '2026-10-01')).toBe('2026-10-01');
  expect(nextPlanDate({ ...plan, startDate: '2026-10-01', anchorDay: 31 }, '2026-10-02')).toBe('2026-11-30');
  expect(nextPlanDate({ ...plan, frequency: 'Once' }, '2026-10-23')).toBeNull();
  expect(nextPlanDate({ ...plan, isActive: false }, '2026-10-23')).toBeNull();
  expect(nextPlanDate({ ...plan, frequency: 'Weekly', startDate: '2026-10-01', interval: 2 }, '2026-10-16')).toBe('2026-10-29');
});
test('API maps server list/summary and preserves typed money', async () => {
  const client = create(); const api = createFinanceApi(client);
  client.defaults.adapter = async (config) => ({ data: JSON.stringify(config.url?.includes('summary') ? summary : { items: [plan], pageNumber: 2, pageSize: 20, totalCount: 21, totalPages: 2 }), status: 200, statusText: 'OK', headers: {}, config });
  const list = await api.list({ pageNumber: 2, pageSize: 20, search: 'Ev' });
  expect(list.totalCount).toBe(21); expect(list.items[0].rowVersion).toBe(plan.rowVersion);
  expect((await api.summary()).upcomingTotalsByCurrency).toEqual(summary.upcomingTotalsByCurrency);
});
test('API update includes rowVersion; delete sends one strong If-Match', async () => {
  const client = create(); const api = createFinanceApi(client); const seen: { method?: string; data: unknown; etag: unknown }[] = [];
  client.defaults.adapter = async (config) => { seen.push({ method: config.method, data: config.data, etag: config.headers['If-Match'] }); return { data: config.method === 'delete' ? '' : JSON.stringify(plan), status: 200, statusText: 'OK', headers: {}, config }; };
  await api.update(plan.id, toPaymentWrite(formDefaults('2026-10-23', plan)), plan.rowVersion); await api.remove(plan.id, plan.rowVersion);
  expect(JSON.parse(seen[0].data as string).rowVersion).toBe(plan.rowVersion); expect(seen[1].etag).toBe(`"${plan.rowVersion}"`);
});
test('a changed session cannot send an old form payload', async () => {
  const client = create(); let sent = false;
  client.defaults.adapter = async () => { sent = true; throw new Error('unexpected'); };
  const api = createFinanceApi(client, { epoch: 1, isCurrent: () => false });
  await expect(api.create(toPaymentWrite(formDefaults('2026-10-23', plan)))).rejects.toThrow('Oturum değişti'); expect(sent).toBe(false);
});
