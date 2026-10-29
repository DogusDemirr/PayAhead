import { beforeEach, afterEach, expect, jest, test } from '@jest/globals';
import { act, renderHook } from '@testing-library/react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { createApiClient } from '../src/services/api';
import { useAvatarMutation } from '../src/features/account/avatar';
import { apiClient, session } from '../src/services/auth-runtime';
import { useSessionStore } from '../src/store/session';
import { ApiError } from '../src/services/api-error';
import { AvatarFailure, avatarMessage, classifyAvatarFailure } from '../src/services/avatar-error';
import { languages } from '../src/i18n/languages';
import { i18next, t, useLanguageStore } from '../src/i18n/runtime';
const { installFormDataPatch } = jest.requireActual<typeof import('expo/src/winter/FormData')>('expo/src/winter/FormData');
const { convertFormDataAsync } = jest.requireActual<typeof import('expo/src/winter/fetch/convertFormData')>('expo/src/winter/fetch/convertFormData');

jest.mock('../src/services/auth-runtime', () => ({ apiClient: require('axios').create(), currentUserKey: () => ['current-user', 1], session: { credential: jest.fn(() => ({ epoch: 1, accessToken: 'fixture-only' })), ensureFreshAccess: async () => undefined, updateUser: jest.fn() } }));
const user = { id: 'aaaaaaaa-aaaa-4aaa-baaa-aaaaaaaaaaaa', email: 'a@example.invalid', displayName: 'Ada' };
beforeEach(() => { jest.clearAllMocks(); jest.spyOn(apiClient, 'get').mockResolvedValue({ status: 200, data: new Uint8Array([255,216,255,217]).buffer, headers: { 'content-type': 'image/jpeg' } }); });
const profile = { ...user, hasAvatar: true, rowVersion: 'AAAAAAAAAAI=' };
afterEach(() => { jest.restoreAllMocks(); void i18next.changeLanguage('tr'); useLanguageStore.setState({ language: 'tr' }); });

test('SDK57 Expo fetch rejects a native URI part before any HTTP request', async () => {
  const RNFormData = jest.requireActual<{ default: typeof FormData }>('react-native/Libraries/Network/FormData').default;
  const body = new (installFormDataPatch(RNFormData))();
  body.append('image', { uri: 'file:///optimized.jpg', type: 'image/jpeg', name: 'avatar.jpg' } as unknown as Blob);
  await expect(convertFormDataAsync(body as unknown as FormData)).rejects.toThrow('Unsupported FormDataPart implementation');
});

test('native avatar explicitly selects XHR and retains file metadata without a fixed boundary header', async () => {
  useSessionStore.setState({ status: 'authenticated', user });
  const originalForm = global.FormData; const RNForm = jest.requireActual<{ default: typeof FormData }>('react-native/Libraries/Network/FormData').default; global.FormData = RNForm;
  const put = jest.spyOn(apiClient, 'put').mockResolvedValue({ data: profile, status: 200 });
  const cache = new QueryClient(); const view = await renderHook(() => useAvatarMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  await act(async () => { await view.result.current.mutateAsync({ kind: 'upload', uri: 'file:///optimized.jpg', rowVersion: 'AAAAAAAAAAE=' }); });
  const [, body, config] = put.mock.calls[0];
  expect(config).toMatchObject({ adapter: 'xhr', headers: { 'If-Match': '"AAAAAAAAAAE="' } });
  expect(config?.headers).not.toHaveProperty('Content-Type');
  const parts = (body as { getParts(): { uri: string; headers: Record<string, string> }[] }).getParts();
  expect(parts).toHaveLength(1); expect(parts[0].uri).toBe('file:///optimized.jpg'); expect(parts[0].headers['content-type']).toBe('image/jpeg'); expect(parts[0].headers['content-disposition']).toContain('name="image"; filename="avatar.jpg"');
  expect(session.updateUser).toHaveBeenCalled(); expect(cache.getQueryData(['finance', user.id, 1, 'profile'])).toEqual(profile); await view.unmount(); cache.clear(); global.FormData = originalForm;
});

test('real Axios XHR adapter passes multipart body and authorization to native networking with no invented boundary', async () => {
  const original = global.XMLHttpRequest; const originalForm = global.FormData;
  const sent: { body?: unknown; headers: Record<string, string> } = { headers: {} };
  class XHR {
    onloadend: (() => void) | null = null; status = 200; statusText = 'OK'; responseText = JSON.stringify(profile);
    open() {} abort() {} setRequestHeader(name: string, value: string) { sent.headers[name] = value; }
    getAllResponseHeaders() { return 'Content-Type: application/json\r\n'; }
    send(body: unknown) { sent.body = body; Promise.resolve().then(() => this.onloadend?.()); }
  }
  global.XMLHttpRequest = XHR as unknown as typeof XMLHttpRequest;
  global.FormData = jest.requireActual<{ default: typeof FormData }>('react-native/Libraries/Network/FormData').default;
  try {
    // Axios detects adapter support when imported. Initialize it after the native globals,
    // matching Expo runtime order rather than Jest's browser FormData environment.
    let makeClient!: typeof createApiClient;
    jest.isolateModules(() => { makeClient = require('../src/services/api').createApiClient; });
    const client = makeClient('http://localhost:5096', { credential: () => ({ epoch: 1, accessToken: 'fixture-only' }), refresh: async () => {}, invalidate: async () => {} });
    const body = new FormData(); body.append('image', { uri: 'file:///optimized.jpg', name: 'avatar.jpg', type: 'image/jpeg' } as unknown as Blob);
    const response = await client.put('/api/users/me/avatar', body, { adapter: 'xhr', headers: { 'If-Match': '"AAAAAAAAAAE="' } });
    expect(response.status).toBe(200); expect(sent.body).toBe(body); expect(sent.headers.Authorization).toBe('Bearer fixture-only');
    expect(Object.keys(sent.headers).some(name => name.toLowerCase() === 'content-type')).toBe(false);
  } finally { global.XMLHttpRequest = original; global.FormData = originalForm; }
});

test('only genuine avatar_changed409 refreshes safely and never automatically retries a write', async () => {
  useSessionStore.setState({ status: 'authenticated', user });
  const put = jest.spyOn(apiClient, 'put').mockRejectedValue(new ApiError('', 409, undefined, undefined, 'http', 'avatar_changed'));
  const get = jest.spyOn(apiClient, 'get').mockResolvedValue({ data: profile, status: 200 });
  const cache = new QueryClient(); const view = await renderHook(() => useAvatarMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'upload', uri: 'file:///optimized.jpg', rowVersion: 'AAAAAAAAAAE=' })).rejects.toMatchObject({ status: 409, code: 'avatar_changed' }); });
  expect(get).toHaveBeenCalledTimes(1); expect(put).toHaveBeenCalledTimes(1); expect(cache.getQueryData(['finance', user.id, 1, 'profile'])).toEqual(profile); await view.unmount(); cache.clear();
});

test.each(languages)('$code distinguishes safe photo errors using status and code', async ({ code }) => {
  await act(async () => { await i18next.changeLanguage(code); useLanguageStore.setState({ language: code }); });
  for (const [error, key] of [
    [new ApiError('', undefined, undefined, undefined, 'network'), 'network'],
    [new ApiError('', 413), 'large'], [new ApiError('', 415), 'invalid'],
    [new ApiError('', 500), 'failed'], [new ApiError('', 409, undefined, undefined, 'http', 'business_rule'), 'failed'],
    [new ApiError('', 500, undefined, undefined, 'http', 'avatar_changed'), 'failed'],
    [new ApiError('', 409, undefined, undefined, 'http', 'avatar_changed'), 'retry'],
  ] as const) expect(avatarMessage(new AvatarFailure('request', error))).toBe(t('settings:avatar.' + key));
  expect(avatarMessage(new AvatarFailure('request', new ApiError('', 401)))).toBe(t('common:m_d1d749ecce55'));
});
test('diagnostics expose only a stage, status and allowlisted code; no URI or raw exception', () => {
  const log = jest.spyOn(console, 'info').mockImplementation(() => {}); classifyAvatarFailure(new Error('file:///private/location.jpg secret'), 'preparation');
  expect(log).toHaveBeenCalledWith('avatar.failure', { stage: 'preparation', status: undefined, code: 'avatar_failed' });
  expect(JSON.stringify(log.mock.calls)).not.toMatch(/private|secret/);
});

test.each([
  { status: 409, code: 'business_rule', expected: 'avatar_failed' },
  { status: 500, code: 'avatar_changed', expected: 'avatar_failed' },
  { status: 500, code: 'avatar_invalid', expected: 'avatar_failed' },
])('HTTP$status/$code does not trigger concurrency recovery', async ({ status, code, expected }) => {
  useSessionStore.setState({ status: 'authenticated', user });
  const put = jest.spyOn(apiClient, 'put').mockRejectedValue(new ApiError('', status, undefined, undefined, 'http', code));
  const get = jest.spyOn(apiClient, 'get'); const cache = new QueryClient(); cache.setQueryData(['finance', user.id, 1, 'profile'], profile);
  const view = await renderHook(() => useAvatarMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'upload', uri: 'file:///optimized.jpg', rowVersion: profile.rowVersion })).rejects.toMatchObject({ code: expected, status }); });
  expect(get).not.toHaveBeenCalled(); expect(put).toHaveBeenCalledTimes(1); expect(cache.getQueryData(['finance', user.id, 1, 'profile'])).toEqual(profile); await view.unmount(); cache.clear();
});
test('HTTP200 with an invalid response is unconfirmed and never presented as successful delivery', async () => {
  useSessionStore.setState({ status: 'authenticated', user });
  jest.mocked(session.updateUser).mockClear();
  jest.spyOn(apiClient, 'put').mockResolvedValue({ status: 200, data: { unexpected: true } });
  const cache = new QueryClient(); cache.setQueryData(['finance', user.id, 1, 'profile'], profile);
  const view = await renderHook(() => useAvatarMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'upload', uri: 'file:///optimized.jpg', rowVersion: profile.rowVersion })).rejects.toMatchObject({ stage: 'response', status: 200, code: 'avatar_unconfirmed' }); });
  expect(session.updateUser).not.toHaveBeenCalled(); expect(cache.getQueryData(['finance', user.id, 1, 'profile'])).toEqual(profile); await view.unmount(); cache.clear();
});
