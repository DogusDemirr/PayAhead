import { test, expect } from '@jest/globals';
import { create } from 'axios';
import { subscriptionDefaults, subscriptionFormSchema, toSubscriptionWrite, consumesTrackingSlot } from '../src/features/subscriptions/validation';
import { createSubscriptionApi, serializeSubscription } from '../src/features/subscriptions/api';
import { parseFinancialJson } from '../src/features/payments/money';
import { subscription } from './subscription-fixtures';
const fields = () => subscriptionDefaults('2026-10-24', subscription);
test('trial end and first paid date are distinct, valid and ordered', () => {
  expect(subscriptionFormSchema.safeParse(fields()).success).toBe(true);
  for (const changes of [{ firstBillingDate: '2026-10-31' }, { trialStartDate: '' }, { trialEndDate: '' }, { trialEndDate: '2026-02-30' }, { startDate: '2026-11-01' }, { trialEndDate: '2026-10-20' }]) expect(subscriptionFormSchema.safeParse({ ...fields(), ...changes }).success).toBe(false);
});
test('backend recurring enums, ISO currencies, precision and intervals are enforced', () => {
  for (const billingFrequency of ['Daily', 'Weekly', 'Monthly', 'Yearly']) expect(subscriptionFormSchema.safeParse({ ...fields(), billingFrequency }).success).toBe(true);
  for (const changes of [{ billingFrequency: 'Once' }, { billingInterval: '0' }, { currencyCode: 'ZZZ' }, { amount: '0' }, { amount: '1,12345' }, { anchorDay: '32' }]) expect(subscriptionFormSchema.safeParse({ ...fields(), ...changes }).success).toBe(false);
  expect(toSubscriptionWrite({ ...fields(), billingFrequency: 'Weekly' }).anchorDay).toBeNull();
});
test('nonrenewing active requires access end; estimates preserve decimals and currencies separately', () => {
  expect(subscriptionFormSchema.safeParse({ ...fields(), status: 'Active', autoRenew: false }).success).toBe(false);
  expect(subscriptionFormSchema.safeParse({ ...fields(), status: 'Active', autoRenew: false, accessEndsOn: '2026-10-31' }).success).toBe(true);
  expect(consumesTrackingSlot({ ...fields(), accessEndsOn: '2026-10-23' }, '2026-10-24')).toBe(false);
  expect(parseFinancialJson('{"estimatedAmount":999999999999999.9999,"currencyCode":"USD"}')).toEqual({ estimatedAmount: '999999999999999.9999', currencyCode: 'USD' });
});
test('request uses exact numeric amount and only backend fields, with update rowVersion', () => {
  const raw = serializeSubscription(toSubscriptionWrite({ ...fields(), name: 'A "name"', amount: '999999999999999,9999' }), subscription.rowVersion);
  expect(raw).toContain('"amount":999999999999999.9999'); expect(JSON.parse(raw).name).toBe('A "name"'); expect(JSON.parse(raw).rowVersion).toBe(subscription.rowVersion);
  expect(JSON.parse(raw).paymentPlanId).toBeUndefined(); expect(JSON.parse(raw).userId).toBeUndefined();
});
test('cancel and delete use current version; no second payment creation', async () => {
  const client = create(); const seen: { method?: string; url?: string; body: unknown; tag: unknown }[] = [];
  client.defaults.adapter = async config => { seen.push({ method: config.method, url: config.url, body: config.data, tag: config.headers['If-Match'] }); return { config, data: config.method === 'delete' ? '' : JSON.stringify(subscription), status: 200, statusText: 'OK', headers: {} }; };
  const api = createSubscriptionApi(client);
  await api.create(toSubscriptionWrite(fields())); await api.update(subscription.id, toSubscriptionWrite(fields()), subscription.rowVersion);
  await api.cancel(subscription.id, '2026-11-10', subscription.rowVersion); await api.remove(subscription.id, subscription.rowVersion);
  expect(seen.map(x => x.url)).toEqual(['/api/subscriptions', `/api/subscriptions/${subscription.id}`, `/api/subscriptions/${subscription.id}/cancel`, `/api/subscriptions/${subscription.id}`]);
  expect(JSON.parse(seen[2].body as string)).toEqual({ accessEndsOn: '2026-11-10', rowVersion: subscription.rowVersion }); expect(seen[3].tag).toBe(`"${subscription.rowVersion}"`);
});
test('list and expiring requests pass server filters and cancellation signal', async () => {
  const client = create(); const signal = new AbortController().signal; const seen: unknown[] = [];
  client.defaults.adapter = async config => { seen.push({ params: config.params, signal: config.signal }); return { config, data: JSON.stringify(config.url?.includes('expiring') ? [] : { items: [subscription], totalCount: 21, totalPages: 2, pageNumber: 2, pageSize: 20 }), status: 200, statusText: 'OK', headers: {} }; };
  const api = createSubscriptionApi(client); const filter = { status: 'Trial' as const, currencyCode: 'TRY', search: 'Music', pageNumber: 2, pageSize: 20 };
  expect((await api.list(filter, signal)).totalCount).toBe(21); await api.trials(3, signal); expect(seen).toEqual([{ params: filter, signal }, { params: { withinDays: 3 }, signal }]);
});
test('session change before preparation prevents writes and after response prevents stale reads', async () => {
  const client = create(); let current = true; let sent = 0;
  client.defaults.adapter = async config => { sent++; current = false; return { config, data: JSON.stringify(subscription), status: 200, statusText: 'OK', headers: {} }; };
  const api = createSubscriptionApi(client, { isCurrent: () => current, prepareWrite: async () => { current = false; } });
  await expect(api.create(toSubscriptionWrite(fields()))).rejects.toMatchObject({ code: 'ERR_CANCELED' }); expect(sent).toBe(0);
  current = true; await expect(api.detail(subscription.id)).rejects.toMatchObject({ code: 'ERR_CANCELED' }); expect(sent).toBe(1);
});
