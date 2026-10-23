import { test, expect, jest, beforeEach } from '@jest/globals';
import { renderHook, act } from '@testing-library/react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';

import { usePaymentMutation, useOccurrenceMutation, financeKey } from '../src/features/payments/queries';
import { useSessionStore } from '../src/store/session';
import { apiClient, session } from '../src/services/auth-runtime';
import { formDefaults, toPaymentWrite } from '../src/features/payments/validation';
import { plan } from './finance-fixtures';
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
