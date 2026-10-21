import { test, expect } from '@jest/globals';
import { AxiosError } from 'axios';
import { createApiClient, ApiError } from '../src/services/api';

test('prepared client injects a supplied token without performing network I/O', async () => {
  const client = createApiClient('https://api.example.test', { credential: () => ({ accessToken: 'example-token', epoch: 1 }), refresh: async () => {}, invalidate: async () => {} });
  client.defaults.adapter = async (config) => ({ data: config.headers.Authorization, status: 200, statusText: 'OK', headers: {}, config });
  expect((await client.get('/api/example')).data).toBe('Bearer example-token');
});
test('missing API URL fails before reaching an adapter', async () => {
  const client = createApiClient(); let reached = false;
  client.defaults.adapter = async () => { reached = true; throw new Error('Unexpected adapter'); };
  await expect(client.get('/example')).rejects.toBeInstanceOf(ApiError);
  expect(reached).toBe(false);
});
test('ProblemDetails becomes a typed error with status and trace ID', async () => {
  const client = createApiClient('https://api.example.test');
  client.defaults.adapter = async (config) => { throw new AxiosError('Rejected', 'ERR_BAD_RESPONSE', config, undefined, { data: { title: 'İstek doğrulanamadı.', traceId: 'example-trace' }, status: 400, statusText: 'Bad Request', headers: {}, config }); };
  await expect(client.get('/api/example')).rejects.toMatchObject({ name: 'ApiError', message: 'İstek doğrulanamadı.', status: 400, traceId: 'example-trace' });
});
