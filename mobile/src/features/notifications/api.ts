import { CanceledError, type AxiosInstance } from 'axios';
import { z } from 'zod';
import { ApiError } from '../../services/api-error';
import type { FinanceContext } from '../payments/api';
import { reminderSchema, deviceSchema, settingsSchema, type ReminderWrite, type SettingsWrite } from './contracts';
export function createNotificationApi(client: AxiosInstance, context: FinanceContext = {}) {
  const check = () => { if (context.isCurrent && !context.isCurrent()) throw new CanceledError('Oturum değişti.'); };
  const config = (signal?: AbortSignal) => { check(); return { signal, sessionEpoch: context.epoch }; };
  function read<T>(schema: z.ZodType<T>, data: unknown) { check(); const result = schema.safeParse(data); if (!result.success) throw new ApiError('Bildirim yanıtı beklenen biçimde değil.'); return result.data; }
  async function write<T>(method: 'post' | 'put' | 'delete', url: string, schema: z.ZodType<T>, data?: unknown) { check(); await context.prepareWrite?.(); check(); const response = await client.request({ ...config(), method, url, data }); check(); return read(schema, response.data); }
  const rules = (id: string) => `/api/payments/${encodeURIComponent(id)}/reminders`;
  return {
    reminders: async (id: string, signal?: AbortSignal) => read(z.array(reminderSchema), (await client.get(rules(id), config(signal))).data),
    saveReminder: (plan: string, body: ReminderWrite, id?: string) => write(id ? 'put' : 'post', rules(plan) + (id ? '/' + encodeURIComponent(id) : ''), reminderSchema, body),
    deleteReminder: (plan: string, id: string) => write('delete', rules(plan) + '/' + encodeURIComponent(id), z.any().transform(() => undefined)),
    settings: async (signal?: AbortSignal) => read(settingsSchema, (await client.get('/api/users/me/settings', config(signal))).data),
    saveSettings: (body: SettingsWrite) => write('put', '/api/users/me/settings', settingsSchema, body),
    devices: async (signal?: AbortSignal) => read(z.array(deviceSchema), (await client.get('/api/devices', config(signal))).data),
    registerDevice: (pushToken: string, platform: 'android' | 'ios', id?: string) => write('post', '/api/devices', deviceSchema, { platform, pushToken, ...(id ? { id } : {}) }),
    deleteDevice: (id: string) => write('delete', `/api/devices/${encodeURIComponent(id)}`, z.any().transform(() => undefined)),
  };
}
