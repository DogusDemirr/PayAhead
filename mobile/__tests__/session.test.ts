import { test, expect, jest } from '@jest/globals';
import { AxiosError, isCancel, type InternalAxiosRequestConfig } from 'axios';
import { SessionManager } from '../src/services/session-manager';
import { createApiClient, ApiError } from '../src/services/api';
import { navigationAccess } from '../src/services/navigation-policy';
import { loginSchema, registerSchema } from '../src/features/auth-validation';
import { QueryClient } from '@tanstack/react-query';
import type { StoredSession } from '../src/services/session-model';
import type { AuthResponse } from '../src/types/api';

const user = { id: 'test-user', email: 'test@example.com', displayName: 'Test' };
const response = (suffix = 'a'): AuthResponse => ({ accessToken: `access-${suffix}`, refreshToken: suffix.repeat(64), accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z', refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z', user, tokenType: 'Bearer' });
function deferred<T>() { let resolve!: (value: T) => void; let reject!: (error: unknown) => void; const promise = new Promise<T>((yes, no) => { resolve = yes; reject = no; }); return { promise, resolve, reject }; }
function setup(saved: StoredSession | null = null, beforeEnd?: (identity: { userId: string; epoch: number; accessToken: string }) => Promise<void>) {
  let record = saved;
  const storage = { read: jest.fn(async () => record), write: jest.fn(async (value: StoredSession) => { record = value; }), clear: jest.fn(async () => { record = null; }) };
  const api = { login: jest.fn(async () => response()), refresh: jest.fn(async (_token: string) => response('b')), logout: jest.fn(async (_token: string) => {}), register: jest.fn(async () => user) };
  const getUser = jest.fn(async () => user); const clearCache = jest.fn();
  const manager = new SessionManager({ api, storage, getUser, clearCache, publish: () => {}, beforeEnd, now: () => Date.parse('2026-10-21T12:00:00Z') });
  return { manager, api, storage, getUser, clearCache, record: () => record };
}
const credentials = { email: 'test@example.com', password: 'Example-password-123' };

test('confirmed profile update changes public session data without storing passwords or rewriting tokens', async () => {
  const s = setup(); await s.manager.login(credentials); const writes = s.storage.write.mock.calls.length; const epoch = s.manager.credential()!.epoch;
  s.manager.updateUser({ ...user, displayName: 'Changed' }, epoch);
  expect(s.manager.getSnapshot().user?.displayName).toBe('Changed'); expect(s.storage.write.mock.calls).toHaveLength(writes);
  expect(() => s.manager.updateUser({ ...user, id: 'another-user' }, epoch)).toThrow();
  await s.manager.logout(); expect(() => s.manager.updateUser(user, epoch)).toThrow();
});
test('logout clears local credentials/cache before waiting for device cleanup', async () => {
  const cleanup = deferred<void>(); const beforeEnd = jest.fn(() => cleanup.promise); const s = setup(null, beforeEnd); await s.manager.login(credentials); const logout = s.manager.logout();
  for (let i = 0; i < 10 && !beforeEnd.mock.calls.length; i++) await Promise.resolve(); expect(s.manager.credential()).toBeNull(); expect(s.record()).toBeNull(); expect(beforeEnd).toHaveBeenCalledWith(expect.objectContaining({ userId: user.id, accessToken: 'access-a' })); cleanup.resolve(); await logout; expect(s.api.logout).toHaveBeenCalledTimes(1);
});
test('offline device cleanup preserves local logout and warns that push may continue', async () => {
  const s = setup(null, async () => { throw new Error('offline'); }); await s.manager.login(credentials); await s.manager.logout(); expect(s.manager.credential()).toBeNull(); expect(s.record()).toBeNull(); expect(s.manager.getSnapshot().notice).toContain('Bildirimler sürebilir');
});
test('device cleanup warning does not hide failed local credential deletion', async () => {
  const s = setup(null, async () => { throw new Error('offline'); }); await s.manager.login(credentials); s.storage.clear.mockRejectedValueOnce(new Error('locked')); await s.manager.logout(); expect(s.manager.getSnapshot().notice).toContain('Cihazdaki oturum kaydı temizlenemedi'); expect(s.manager.getSnapshot().notice).toContain('Bildirimler sürebilir');
});
function unauthorized(config: InternalAxiosRequestConfig) { return new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, undefined, { status: 401, statusText: '', data: {}, headers: {}, config }); }

test('login requires a password but does not impose registration length', () => {
  expect(loginSchema.safeParse({ ...credentials, password: '', displayName: '' }).success).toBe(false);
  expect(loginSchema.safeParse({ ...credentials, password: 'short', displayName: '' }).success).toBe(true);
});
test('registration requires a 12-character password and a nonblank name', () => {
  expect(registerSchema.safeParse({ ...credentials, password: 'short', displayName: 'Test' }).success).toBe(false);
  expect(registerSchema.safeParse({ ...credentials, displayName: '  ' }).success).toBe(false);
  expect(registerSchema.safeParse({ ...credentials, confirmation: credentials.password, displayName: 'Test' }).success).toBe(true);
});
test('successful login persists both tokens and verifies current user before admission', async () => {
  const s = setup(); await s.manager.login(credentials);
  expect(s.record()?.refreshToken).toBe('a'.repeat(64)); expect(s.getUser).toHaveBeenCalledTimes(1);
  expect(s.manager.getSnapshot()).toMatchObject({ status: 'authenticated', user });
  expect(s.manager.getSnapshot()).not.toHaveProperty('accessToken');
});
test('failed credentials leave no local session', async () => {
  const s = setup(); s.api.login.mockRejectedValueOnce(new ApiError('Credentials', 401));
  await expect(s.manager.login(credentials)).rejects.toMatchObject({ status: 401 });
  expect(s.manager.credential()).toBeNull(); expect(s.record()).toBeNull();
});
test('overlapping login attempts cannot create two server sessions', async () => {
  const s = setup(); const pending = deferred<AuthResponse>(); s.api.login.mockReturnValueOnce(pending.promise);
  const first = s.manager.login(credentials);
  await expect(s.manager.login(credentials)).rejects.toThrow('Giriş işlemi sürüyor.');
  pending.resolve(response()); await first; expect(s.api.login).toHaveBeenCalledTimes(1);
});
test('storage write failure never opens protected navigation', async () => {
  const s = setup(); s.storage.write.mockRejectedValueOnce(new Error('storage'));
  await expect(s.manager.login(credentials)).rejects.toThrow('storage');
  expect(navigationAccess(s.manager.getSnapshot().status).protected).toBe(false);
  expect(s.api.logout).toHaveBeenCalledWith(response().refreshToken);
});
test('restore is single-flight, reads secure storage and verifies /me once', async () => {
  const s = setup(response()); await Promise.all([s.manager.restore(), s.manager.restore()]);
  expect(s.storage.read).toHaveBeenCalledTimes(1); expect(s.getUser).toHaveBeenCalledTimes(1);
  expect(s.manager.getSnapshot().status).toBe('authenticated');
});
test('expired access is rotated before current-user restoration', async () => {
  const s = setup({ ...response(), accessTokenExpiresAtUtc: '2020-01-01T00:00:00Z' }); await s.manager.restore();
  expect(s.api.refresh).toHaveBeenCalledTimes(1); expect(s.manager.credential()?.accessToken).toBe('access-b');
});
test('expired refresh clears session without sending an expired rotation', async () => {
  const s = setup({ ...response(), refreshTokenExpiresAtUtc: '2020-01-01T00:00:00Z' }); await s.manager.restore();
  expect(s.api.refresh).not.toHaveBeenCalled(); expect(s.record()).toBeNull();
});
test('refresh success atomically replaces the pair; failure clears session and cache', async () => {
  const s = setup(); await s.manager.login(credentials); await s.manager.refresh();
  expect(s.record()).toMatchObject({ accessToken: 'access-b', refreshToken: 'b'.repeat(64) });
  s.api.refresh.mockRejectedValueOnce(new ApiError('Rejected', 401));
  await expect(s.manager.refresh()).rejects.toMatchObject({ status: 401 });
  expect(s.manager.getSnapshot().status).toBe('anonymous'); expect(s.record()).toBeNull(); expect(s.clearCache).toHaveBeenCalled();
});
test('concurrent 401 requests and a late old-token 401 use one rotation', async () => {
  const s = setup(); await s.manager.login(credentials);
  const rotation = deferred<AuthResponse>(); s.api.refresh.mockReturnValueOnce(rotation.promise);
  const late = deferred<void>(); const client = createApiClient('https://api.example.test', s.manager);
  client.defaults.adapter = async (config) => {
    if (config.headers.Authorization === 'Bearer access-a') { if (config.url === '/api/late') await late.promise; throw unauthorized(config); }
    return { data: user, status: 200, statusText: 'OK', headers: {}, config };
  };
  const first = client.get('/api/first'); const second = client.get('/api/second'); const third = client.get('/api/late');
  // Resolve only once the shared rotation has actually started.
  await new Promise<void>((resolve) => { const poll = () => s.api.refresh.mock.calls.length ? resolve() : setTimeout(poll, 0); poll(); });
  rotation.resolve(response('b')); await Promise.all([first, second]); late.resolve(); await third;
  expect(s.api.refresh).toHaveBeenCalledTimes(1); expect(s.api.refresh).toHaveBeenCalledWith('a'.repeat(64));
});
test('a repeated 401 after refresh cannot recurse', async () => {
  const s = setup(); await s.manager.login(credentials); const client = createApiClient('https://api.example.test', s.manager);
  client.defaults.adapter = async (config) => { throw unauthorized(config); };
  await expect(client.get('/api/users/me')).rejects.toMatchObject({ status: 401 });
  expect(s.api.refresh).toHaveBeenCalledTimes(1); expect(s.manager.credential()).toBeNull();
});
test('canceled request waiting on refresh is never replayed', async () => {
  const s = setup(); await s.manager.login(credentials); const rotation = deferred<AuthResponse>(); s.api.refresh.mockReturnValueOnce(rotation.promise);
  const client = createApiClient('https://api.example.test', s.manager); let sent = 0;
  client.defaults.adapter = async (config) => { sent++; throw unauthorized(config); };
  const controller = new AbortController(); const request = client.get('/api/users/me', { signal: controller.signal }).catch((error) => error);
  await new Promise<void>((resolve) => { const poll = () => s.api.refresh.mock.calls.length ? resolve() : setTimeout(poll, 0); poll(); });
  controller.abort(); rotation.resolve(response('b'));
  expect(isCancel(await request)).toBe(true); expect(sent).toBe(1);
});
test('logout fences a late rotation and revokes the old family member', async () => {
  const s = setup(); await s.manager.login(credentials); const rotation = deferred<AuthResponse>(); s.api.refresh.mockReturnValueOnce(rotation.promise);
  const pending = s.manager.refresh().catch((error) => error); await s.manager.logout(); rotation.resolve(response('b')); await pending;
  expect(s.api.logout).toHaveBeenCalledWith('a'.repeat(64)); expect(s.record()).toBeNull(); expect(s.manager.credential()).toBeNull();
});
test('offline logout clears local tokens/cache and reports server revocation uncertainty', async () => {
  const s = setup(); await s.manager.login(credentials); s.api.logout.mockRejectedValueOnce(new ApiError('Offline'));
  await s.manager.logout(); expect(s.record()).toBeNull(); expect(s.manager.getSnapshot().notice).toContain('sunucu iptali doğrulanamadı');
});
test('invalid current user during restore fails closed', async () => {
  const s = setup(response()); s.getUser.mockRejectedValueOnce(new ApiError('Inactive', 401)); await s.manager.restore();
  expect(s.manager.getSnapshot().status).toBe('anonymous'); expect(s.record()).toBeNull();
});
test('navigation waits for restore and removes access after logout for Android back/deep links', () => {
  expect(navigationAccess('restoring')).toEqual({ ready: false, protected: false, auth: false });
  expect(navigationAccess('authenticated').protected).toBe(true);
  expect(navigationAccess('anonymous')).toEqual({ ready: true, protected: false, auth: true });
});
test('production refuses HTTP and bearer tokens cannot be sent to another origin', async () => {
  const s = setup(); await s.manager.login(credentials);
  await expect(createApiClient('http://192.168.1.10:5080', s.manager, false).get('/api/users/me')).rejects.toMatchObject({ kind: 'configuration' });
  await expect(createApiClient('https://api.example.test', s.manager).get('https://other.example/api/users/me')).rejects.toMatchObject({ kind: 'configuration' });
});
test('timeout is mapped without exposing config or token bodies', async () => {
  const client = createApiClient('https://api.example.test'); client.defaults.adapter = async (config) => { throw new AxiosError('raw private detail', 'ECONNABORTED', config); };
  await expect(client.post('/api/auth/login', credentials)).rejects.toMatchObject({ kind: 'timeout' });
});
test('logout and another account login clear all previous financial data', async () => {
  const cache = new QueryClient({ defaultOptions: { queries: { gcTime: Infinity } } }); const s = setup();
  s.clearCache.mockImplementation(() => { cache.clear(); }); await s.manager.login(credentials);
  const oldEpoch = s.manager.credential()!.epoch; cache.setQueryData(['finance', user.id, oldEpoch, 'plans'], { privateAmount: '12.00' });
  await s.manager.logout(); expect(cache.getQueryCache().getAll()).toHaveLength(0);
  const second = { ...user, id: 'another-user', email: 'second@example.com' };
  s.api.login.mockResolvedValueOnce({ ...response('c'), user: second }); s.getUser.mockResolvedValueOnce(second);
  await s.manager.login({ ...credentials, email: second.email }); expect(s.manager.getSnapshot().user?.id).toBe(second.id);
  expect(s.manager.credential()!.epoch).not.toBe(oldEpoch); expect(cache.getQueryData(['finance', user.id, oldEpoch, 'plans'])).toBeUndefined(); cache.clear();
});
test('write 401 rotates centrally but never automatically replays a mutation', async () => {
  const s = setup(); await s.manager.login(credentials); const client = createApiClient('https://api.example.test', s.manager); let sent = 0;
  client.defaults.adapter = async (config) => { sent++; throw unauthorized(config); };
  await expect(client.post('/api/payments', { name: 'Test' })).rejects.toMatchObject({ status: 401, kind: 'session' });
  expect(sent).toBe(1); expect(s.api.refresh).toHaveBeenCalledTimes(1);
});
