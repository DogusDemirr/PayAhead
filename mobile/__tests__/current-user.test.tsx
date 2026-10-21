import { test, expect, jest } from '@jest/globals';
import { renderHook, waitFor, act } from '@testing-library/react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useCurrentUser } from '../src/hooks/use-current-user';
import { useSessionStore } from '../src/store/session';
import { fetchCurrentUser } from '../src/services/auth-runtime';
jest.mock('../src/services/auth-runtime', () => ({ currentUserKey: () => ['current-user', 1], fetchCurrentUser: jest.fn() }));
test('current user is fetched via TanStack Query only for an authenticated session', async () => {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } });
  useSessionStore.setState({ status: 'anonymous', user: null, notice: null });
  const user = { id: 'from-backend', displayName: 'Actual response', email: 'test@example.com' };
  jest.mocked(fetchCurrentUser).mockResolvedValue(user);
  const view = await renderHook(() => useCurrentUser(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={client}>{children}</QueryClientProvider> });
  expect(fetchCurrentUser).not.toHaveBeenCalled();
  // State updates are flushed through the library's async act.
  await act(async () => { useSessionStore.setState({ status: 'authenticated' }); });
  await waitFor(() => expect(client.getQueryData(['current-user', 1])).toEqual(user));
  expect(fetchCurrentUser).toHaveBeenCalledTimes(1); await view.unmount(); client.clear();
});
