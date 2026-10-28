import { test, expect, jest, beforeEach } from '@jest/globals';
import { renderHook, act } from '@testing-library/react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';

import { usePaymentMutation, useOccurrenceMutation, financeKey } from '../src/features/payments/queries';
import { useSessionStore } from '../src/store/session';
import { apiClient, session } from '../src/services/auth-runtime';
import { formDefaults, toPaymentWrite } from '../src/features/payments/validation';
import { plan } from './finance-fixtures';
import { useSubscriptionMutation } from '../src/features/subscriptions/queries';
import { subscription } from './subscription-fixtures';
import { subscriptionDefaults, toSubscriptionWrite } from '../src/features/subscriptions/validation';
import { useNotificationMutation } from '../src/features/notifications/queries';
jest.mock('../src/services/auth-runtime', () => ({ apiClient: require('axios').create(), session: { credential: jest.fn(() => ({ epoch: 1, accessToken: 'test' })), ensureFreshAccess: async () => {} } }));
beforeEach(() => { jest.mocked(session.credential).mockReturnValue({ epoch: 1, accessToken: 'test' }); useSessionStore.setState({ status: 'authenticated', user: { id: 'user-a', email: 'test@example.com', displayName: 'Test' }, notice: null }); });
test('successful mutation invalidates only the current user/session financial caches', async () => {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity, retry: false } } });
  const own = [...financeKey('user-a', 1), 'summary']; const other = [...financeKey('user-b', 2), 'summary'];
  client.setQueryData(own, { total: 'old' }); client.setQueryData(other, { total: 'foreign' });
  apiClient.defaults.adapter = async (config) => ({ data: JSON.stringify(plan), status: 201, statusText: 'Created', headers: {}, config });
  const view = await renderHook(() => usePaymentMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  await act(async () => { await view.result.current.mutateAsync({ kind: 'create', body: toPaymentWrite(formDefaults('2026-10-23', plan)) }); });
  expect(client.getQueryState(own)?.isInvalidated).toBe(true); expect(client.getQueryState(other)?.isInvalidated).toBe(false);
  await view.unmount(); client.clear();
});
test('session change during invalidation cancels the old mutation continuation', async () => {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity, retry: false } } });
  jest.spyOn(client, 'invalidateQueries').mockImplementation(async () => { jest.mocked(session.credential).mockReturnValue({ epoch: 2, accessToken: 'new-session' }); });
  apiClient.defaults.adapter = async (config) => ({ data: JSON.stringify(plan), status: 201, statusText: 'Created', headers: {}, config });
  const view = await renderHook(() => usePaymentMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'create', body: toPaymentWrite(formDefaults('2026-10-23', plan)) })).rejects.toMatchObject({ code: 'ERR_CANCELED' }); });
  await view.unmount(); client.clear();
});

test('occurrence success invalidates dashboard calendar plans history analytics only for the current session', async () => {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity, retry: false } } });
  const own = ['summary', 'calendar', 'plans', 'history', 'analytics'].map(kind => [...financeKey('user-a', 1), kind]);
  const other = [...financeKey('user-b', 2), 'calendar']; own.forEach(key => client.setQueryData(key, {})); client.setQueryData(other, {});
  apiClient.defaults.adapter = async config => ({ data: JSON.stringify({ id: null, paymentPlanId: plan.id, dueDate: '2026-10-31', status: 'Pending', actualAmount: null, paidAtUtc: null, createdAtUtc: null, rowVersion: null, isRecorded: false }), status: 200, statusText: 'OK', headers: {}, config });
  const view = await renderHook(() => useOccurrenceMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  await act(async () => { await view.result.current.mutateAsync({ id: plan.id, date: '2026-10-31', status: 'Pending', rowVersion: null }); });
  own.forEach(key => expect(client.getQueryState(key)?.isInvalidated).toBe(true)); expect(client.getQueryState(other)?.isInvalidated).toBe(false);
  await view.unmount(); client.clear();
});

test.each(['create', 'update', 'cancel', 'delete'] as const)('subscription %s invalidates all own finance caches and leaves other users intact', async kind => {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity, retry: false } } });
  const keys = ['summary', 'calendar', 'plans', 'history', 'analytics', 'entitlements', 'subscriptions'].map(name => [...financeKey('user-a', 1), name]);
  keys.forEach(key => client.setQueryData(key, {})); const other = [...financeKey('user-b', 2), 'subscriptions']; client.setQueryData(other, {});
  apiClient.defaults.adapter = async config => ({ config, data: kind === 'delete' ? '' : JSON.stringify(subscription), status: 200, statusText: 'OK', headers: {} });
  const view = await renderHook(() => useSubscriptionMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  const body = toSubscriptionWrite(subscriptionDefaults('2026-10-24', subscription));
  await act(async () => { await view.result.current.mutateAsync(kind === 'create' ? { kind, body } : kind === 'update' ? { kind, id: subscription.id, body, rowVersion: subscription.rowVersion } : kind === 'cancel' ? { kind, id: subscription.id, accessEndsOn: '2026-11-01', rowVersion: subscription.rowVersion } : { kind, id: subscription.id, rowVersion: subscription.rowVersion }); });
  keys.forEach(key => expect(client.getQueryState(key)?.isInvalidated).toBe(true)); expect(client.getQueryState(other)?.isInvalidated).toBe(false); await view.unmount(); client.clear();
});
test('subscription mutation cannot navigate with a previous session result after invalidation', async () => {
  const client = new QueryClient(); jest.spyOn(client, 'invalidateQueries').mockImplementation(async () => { jest.mocked(session.credential).mockReturnValue({ epoch: 2, accessToken: 'new-session' }); });
  apiClient.defaults.adapter = async config => ({ config, data: JSON.stringify(subscription), status: 200, statusText: 'OK', headers: {} });
  const view = await renderHook(() => useSubscriptionMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'cancel', id: subscription.id, rowVersion: subscription.rowVersion, accessEndsOn: '2026-11-01' })).rejects.toMatchObject({ code: 'ERR_CANCELED' }); }); await view.unmount(); client.clear();
});
test('reminder mutation invalidates own settings/rules and finance only within current user epoch', async () => {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity, retry: false } } }); const own = [...financeKey('user-a', 1), 'notifications', 'reminders', plan.id]; const other = [...financeKey('user-b', 2), 'notifications', 'reminders', plan.id]; client.setQueryData(own, []); client.setQueryData(other, []);
  apiClient.defaults.adapter = async config => ({ config, data: JSON.stringify({ id: plan.id, paymentPlanId: plan.id, daysBefore: 1, timeOfDay: '09:00', isEnabled: true }), status: 201, statusText: 'Created', headers: {} });
  const view = await renderHook(() => useNotificationMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> }); await act(async () => { await view.result.current.mutateAsync({ kind: 'saveReminder', plan: plan.id, body: { daysBefore: 1, timeOfDay: '09:00', isEnabled: true } }); }); expect(client.getQueryState(own)?.isInvalidated).toBe(true); expect(client.getQueryState(other)?.isInvalidated).toBe(false); await view.unmount(); client.clear();
});


test('deactivation immediately installs the returned version and confirmed delete removes only own caches', async () => {
  const client = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity, retry: false } } });
  const detail = [...financeKey('user-a', 1), 'plan', plan.id];
  const list = [...financeKey('user-a', 1), 'plans', {}];
  const foreign = [...financeKey('user-b', 2), 'plan', plan.id];
  client.setQueryData(detail, plan); client.setQueryData(foreign, plan);
  client.setQueryData(list, { items: [plan], totalCount: 1, totalPages: 1, pageSize: 20 });
  const updated = { ...plan, isActive: false, rowVersion: 'AAAAAAAAAAI=' };
  let sentVersion: unknown;
  apiClient.defaults.adapter = async config => { sentVersion = config.headers['If-Match']; return { data: config.method === 'delete' ? '' : JSON.stringify(updated), status: 200, statusText: 'OK', headers: {}, config }; };
  const view = await renderHook(() => usePaymentMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  await act(async () => { await view.result.current.mutateAsync({ kind: 'update', id: plan.id, rowVersion: plan.rowVersion, body: toPaymentWrite(formDefaults('2026-10-23', plan)) }); });
  expect(client.getQueryData(detail)).toEqual(updated);
  await act(async () => { await view.result.current.mutateAsync({ kind: 'delete', id: plan.id, rowVersion: updated.rowVersion }); });
  expect(sentVersion).toBe('"' + updated.rowVersion + '"'); expect(client.getQueryData(detail)).toBeUndefined();
  expect(client.getQueryData(list)).toMatchObject({ items: [], totalCount: 0 }); expect(client.getQueryData(foreign)).toEqual(plan);
  await view.unmount(); client.clear();
});

test('failed deletion preserves the cached plan and list', async () => {
  const client = new QueryClient(); const detail = [...financeKey('user-a', 1), 'plan', plan.id]; const list = [...financeKey('user-a', 1), 'plans', {}];
  client.setQueryData(detail, plan); client.setQueryData(list, { items: [plan], totalCount: 1 });
  apiClient.defaults.adapter = async () => { throw new Error('request rejected'); };
  const view = await renderHook(() => usePaymentMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'delete', id: plan.id, rowVersion: plan.rowVersion })).rejects.toThrow(); });
  expect(client.getQueryData(detail)).toEqual(plan); expect(client.getQueryData(list)).toEqual({ items: [plan], totalCount: 1 }); await view.unmount(); client.clear();
});
