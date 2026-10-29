import { expect, jest, test } from '@jest/globals';
import { AxiosError } from 'axios';
import { avatarSourceOptions, jpegDataSource } from '../src/features/account/avatar-source';
import { createApiClient } from '../src/services/api';
const picture = new Uint8Array([255, 216, 255, 217]).buffer; // Protocol fixture, real JPEG decode is covered by SQL/HTTP tests.
test('JPEG source is encoded exactly, including padding, without credentials in a native URL', () => {
  for (const size of [0, 1, 2, 3, 7, 8193]) {
    const bytes = new Uint8Array(size + 4); bytes.set([255, 216]); bytes.set([255, 217], bytes.length - 2);
    expect(jpegDataSource(bytes.buffer, 'image/jpeg')).toBe('data:image/jpeg;base64,' + Buffer.from(bytes).toString('base64'));
  }
});
test('non-image, wrong MIME and oversized data cannot be passed to the bitmap', () => {
  for (const [data, type] of [[picture, 'application/json'], [new ArrayBuffer(4), 'image/jpeg'], [new ArrayBuffer(2 * 1024 * 1024 + 1), 'image/jpeg']] as const)
    expect(() => jpegDataSource(data, type)).toThrow();
});
test('avatar read uses centralized401 refresh/replay before producing an in-memory picture', async () => {
  let token = 'expired-fixture'; let reads = 0;
  const refresh = jest.fn(async () => { token = 'fresh-fixture'; });
  const client = createApiClient('http://localhost:5080', { credential: () => ({ epoch: 7, accessToken: token }), refresh, invalidate: jest.fn(async () => {}) });
  client.defaults.adapter = async config => {
    reads++; expect(config.headers.Authorization).toBe('Bearer ' + token);
    if (reads === 1) throw new AxiosError('fixture', undefined, config, undefined, { config, status: 401, statusText: 'Unauthorized', headers: {}, data: '' });
    expect(config.responseType).toBe('arraybuffer'); return { config, status: 200, statusText: 'OK', headers: { 'content-type': 'image/jpeg' }, data: picture };
  };
  const options = avatarSourceOptions(client, { id: 'owner', epoch: 7, version: 'version', isCurrent: () => true, prepare: async () => {} });
  expect(await options.queryFn({ signal: new AbortController().signal })).toBe('data:image/jpeg;base64,/9j/2Q=='); expect(reads).toBe(2); expect(refresh).toHaveBeenCalledTimes(1);
  expect(options.queryKey).toEqual(['finance', 'owner', 7, 'avatar', 'version']);
});
test('a late binary response from an old session is discarded', async () => {
  let current = true;
  const client = createApiClient('http://localhost:5080');
  client.defaults.adapter = async config => { current = false; return { config, status: 200, statusText: 'OK', headers: { 'content-type': 'image/jpeg' }, data: picture }; };
  const options = avatarSourceOptions(client, { id: 'owner', epoch: 1, version: 'v', isCurrent: () => current, prepare: async () => {} });
  await expect(options.queryFn({ signal: new AbortController().signal })).rejects.toMatchObject({ code: 'ERR_CANCELED' });
});
