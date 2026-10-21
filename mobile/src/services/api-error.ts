import { isAxiosError, isCancel } from 'axios';
import type { ProblemDetails } from '../types/api';

export class ApiError extends Error {
  constructor(message: string, public readonly status?: number, public readonly traceId?: string,
    public readonly errors?: Record<string, string[]>, public readonly kind: 'http' | 'network' | 'timeout' | 'configuration' | 'session' = 'http') {
    super(message); this.name = 'ApiError';
  }
}
export function mapApiError(error: unknown): Error {
  if (error instanceof ApiError || isCancel(error)) return error;
  if (!isAxiosError<ProblemDetails>(error)) return new ApiError('İşlem tamamlanamadı. Tekrar deneyebilirsin.');
  if (error.code === 'ECONNABORTED' || error.code === 'ETIMEDOUT') return new ApiError('Bağlantı zaman aşımına uğradı. Tekrar dene.', undefined, undefined, undefined, 'timeout');
  if (!error.response) return new ApiError('Sunucuya ulaşılamıyor. İnternet bağlantını ve API adresini kontrol et.', undefined, undefined, undefined, 'network');
  const { status, data } = error.response;
  const messages: Record<number, string> = { 401: 'E-posta veya şifre hatalı ya da oturum artık geçerli değil.', 409: 'Bu e-posta ile kayıt oluşturulamıyor.', 429: 'Çok fazla deneme yapıldı. Biraz bekleyip tekrar dene.' };
  return new ApiError(messages[status] ?? (status >= 500 ? 'Sunucu şu anda kullanılamıyor. Daha sonra tekrar dene.' : data?.title ?? 'İstek tamamlanamadı.'), status, data?.traceId, data?.errors);
}
