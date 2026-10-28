import { beforeEach, expect, jest, test } from '@jest/globals';
import { act, renderHook } from '@testing-library/react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useAccountMutation } from '../src/features/account/queries';
import { apiClient, currentUserKey, session } from '../src/services/auth-runtime';
import { useSessionStore } from '../src/store/session';
import { t } from '../src/i18n/runtime';
jest.mock('../src/services/auth-runtime', () => ({ apiClient: require('axios').create(), currentUserKey: () => ['current-user', 1], session: { credential: jest.fn(() => ({ epoch: 1, accessToken: 'fixture-only' })), ensureFreshAccess: async () => undefined, updateUser: jest.fn(), invalidate: jest.fn(async () => undefined) } }));
const user = { id: 'aaaaaaaa-aaaa-4aaa-baaa-aaaaaaaaaaaa', email: 'test@example.invalid', displayName: 'Changed' };
beforeEach(() => { jest.mocked(session.credential).mockReturnValue({ epoch: 1, accessToken: 'fixture-only' }); useSessionStore.setState({ status: 'authenticated', user }); jest.mocked(session.updateUser).mockClear(); jest.mocked(session.invalidate).mockClear(); });
async function setup() {
  const cache = new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } });
  const view = await renderHook(() => useAccountMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  return { cache, view };
}
test('profile writes only editable fields, updates current caches and preserves other accounts', async () => {
  let body: unknown;
  apiClient.defaults.adapter = async config => { body = JSON.parse(config.data as string); return { data: { ...user, rowVersion: 'AAAAAAAAAAI=' }, status: 200, statusText: 'OK', headers: {}, config }; };
  const { cache, view } = await setup(); const foreign = ['finance', 'other-user', 2, 'profile']; cache.setQueryData(foreign, { displayName: 'Foreign' });
  await act(async () => { await view.result.current.mutateAsync({ kind: 'profile', displayName: 'Changed', rowVersion: 'AAAAAAAAAAE=' }); });
  expect(body).toEqual({ displayName: 'Changed', rowVersion: 'AAAAAAAAAAE=' }); expect(session.updateUser).toHaveBeenCalledWith({ ...user, hasAvatar: false, avatarVersion: 'AAAAAAAAAAI=' }, 1);
  expect(cache.getQueryData(currentUserKey())).toEqual({ ...user, hasAvatar: false, avatarVersion: 'AAAAAAAAAAI=' }); expect(cache.getQueryData(foreign)).toEqual({ displayName: 'Foreign' }); cache.clear();
});
test('late response from a previous session cannot update user or caches', async () => {
  apiClient.defaults.adapter = async config => { jest.mocked(session.credential).mockReturnValue({ epoch: 2, accessToken: 'new' }); return { data: { ...user, rowVersion: 'AAAAAAAAAAI=' }, status: 200, statusText: 'OK', headers: {}, config }; };
  const { cache, view } = await setup(); await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'profile', displayName: 'Changed', rowVersion: 'AAAAAAAAAAE=' })).rejects.toThrow(); });
  expect(session.updateUser).not.toHaveBeenCalled(); expect(cache.getQueryData(currentUserKey())).toBeUndefined(); cache.clear();
});
test.each(['password', 'delete'] as const)('%s success clears the session with the correct confirmed notice', async kind => {
  apiClient.defaults.adapter = async config => ({ data: '', status: 204, statusText: 'No Content', headers: {}, config });
  const { cache, view } = await setup(); await act(async () => { await view.result.current.mutateAsync(kind === 'password' ? { kind, currentPassword: 'old-password!', newPassword: 'new-password!', confirmation: 'new-password!' } : { kind, currentPassword: 'old-password!' }); });
  expect(session.invalidate).toHaveBeenCalledWith(t('settings:profile.' + (kind === 'password' ? 'passwordChanged' : 'deleteSuccess'))); cache.clear();
});
