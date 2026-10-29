import { CanceledError, type AxiosInstance } from 'axios';
import { ApiError } from '../../services/api-error';

export function jpegDataSource(data: unknown, contentType: unknown): string {
  const bytes = data instanceof ArrayBuffer ? new Uint8Array(data)
    : ArrayBuffer.isView(data) ? new Uint8Array(data.buffer, data.byteOffset, data.byteLength) : null;
  if (String(contentType).split(';')[0].trim().toLowerCase() !== 'image/jpeg' || !bytes || bytes.length < 4 || bytes.length > 2 * 1024 * 1024
    || bytes[0] !== 255 || bytes[1] !== 216 || bytes[bytes.length - 2] !== 255 || bytes[bytes.length - 1] !== 217)
    throw new ApiError('', 200, undefined, undefined, 'http', 'avatar_unconfirmed');
  // Hermes-safe bounded base64 encoding; no Buffer, binary fetch URL or credentials in the image source.
  const alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/'; const chunks: string[] = []; let chunk = '';
  for (let i = 0; i < bytes.length; i += 3) {
    const value = bytes[i] << 16 | (bytes[i + 1] ?? 0) << 8 | (bytes[i + 2] ?? 0);
    chunk += alphabet[value >>> 18 & 63] + alphabet[value >>> 12 & 63] + (i + 1 < bytes.length ? alphabet[value >>> 6 & 63] : '=') + (i + 2 < bytes.length ? alphabet[value & 63] : '=');
    if (chunk.length >= 8192) { chunks.push(chunk); chunk = ''; }
  }
  chunks.push(chunk); return 'data:image/jpeg;base64,' + chunks.join('');
}
export function avatarSourceOptions(client: AxiosInstance, scope: { id: string; epoch: number; version: string; isCurrent(): boolean; prepare(): Promise<void> }) {
  return {
    queryKey: ['finance', scope.id, scope.epoch, 'avatar', scope.version] as const,
    staleTime: Infinity, gcTime: 30000, retry: false as const,
    queryFn: async ({ signal }: { signal: AbortSignal }) => {
      const check = () => { if (!scope.isCurrent()) throw new CanceledError(); };
      check(); await scope.prepare(); check();
      try {
        // Reads use the same session-aware client as JSON APIs, including its single-flight refresh/replay.
        const response = await client.get('/api/users/me/avatar', { responseType: 'arraybuffer', signal, headers: { Accept: 'image/jpeg', 'Cache-Control': 'no-cache' }, params: { v: scope.version }, ...{ sessionEpoch: scope.epoch } });
        check(); return jpegDataSource(response.data, response.headers['content-type']);
      } catch (error) {
        if (__DEV__ && error instanceof ApiError) console.info('avatar.display', { outcome: 'read_error', status: error.status });
        throw error;
      }
    },
  };
}
