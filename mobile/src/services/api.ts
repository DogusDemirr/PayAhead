import { create, CanceledError, type InternalAxiosRequestConfig } from 'axios';
import { ApiError, mapApiError } from './api-error';
export { ApiError } from './api-error';

export interface SessionAccess {
  credential(): { accessToken: string; epoch: number } | null;
  refresh(): Promise<void>;
  invalidate(message?: string): Promise<void>;
}
type Request = InternalAxiosRequestConfig & { sessionEpoch?: number; sentToken?: string; replayed?: boolean };

export function createApiClient(baseURL?: string, session?: SessionAccess, development = __DEV__) {
  const client = create({ baseURL, timeout: 15000, headers: { Accept: 'application/json' } });
  client.interceptors.request.use((config: Request) => {
    let base: URL;
    try { base = new URL(baseURL ?? ''); } catch { throw new ApiError('API adresi yapılandırılmamış.', undefined, undefined, undefined, 'configuration'); }
    if ((!development && base.protocol !== 'https:') || !['http:', 'https:'].includes(base.protocol) || base.username || base.password || base.search || base.hash)
      throw new ApiError('API için geçerli, production ortamında HTTPS bir adres kullan.', undefined, undefined, undefined, 'configuration');
    // Only same-base relative API paths may receive a bearer token.
    if (!config.url?.startsWith('/api/') || config.url.includes('://') || config.url.includes('\\') || config.baseURL !== baseURL || config.url.includes('..'))
      throw new ApiError('API isteği izin verilen adresin dışında.', undefined, undefined, undefined, 'configuration');
    if (config.signal?.aborted) throw new CanceledError();
    if (session) {
      const credential = session.credential();
      if (!credential || (config.sessionEpoch !== undefined && config.sessionEpoch !== credential.epoch)) throw new CanceledError('Oturum değişti.');
      config.sessionEpoch = credential.epoch; config.sentToken = credential.accessToken;
      config.headers.Authorization = `Bearer ${credential.accessToken}`;
    } else config.headers.delete('Authorization');
    return config;
  });
  client.interceptors.response.use((response) => {
    const config = response.config as Request;
    if (session && session.credential()?.epoch !== config.sessionEpoch) throw new CanceledError('Oturum değişti.');
    return response;
  }, async (error: unknown) => {
    const failed = error as { response?: { status: number }; config?: Request };
    const config = failed.config;
    if (session && config && failed.response?.status === 401) {
      if (config.signal?.aborted || session.credential()?.epoch !== config.sessionEpoch) throw new CanceledError();
      if (config.replayed) { await session.invalidate('Oturumun artık geçerli değil. Yeniden giriş yap.'); throw mapApiError(error); }
      // Phase 2 protected API is GET /me. Never silently replay a future mutation.
      if (!['get', 'head', 'options'].includes(config.method ?? 'get')) throw mapApiError(error);
      config.replayed = true;
      if (session.credential()?.accessToken === config.sentToken) await session.refresh();
      if (config.signal?.aborted || session.credential()?.epoch !== config.sessionEpoch) throw new CanceledError();
      return client.request(config);
    }
    throw mapApiError(error);
  });
  return client;
}
