import { test, expect, jest } from '@jest/globals';
import { create } from 'axios';
import { reminderFormSchema, settingsFormSchema, notificationTargetSchema, validTimeZone } from '../src/features/notifications/contracts';
import { createNotificationApi } from '../src/features/notifications/api';
import { DeviceManager, type DeviceBinding, type DeviceIdentity } from '../src/features/notifications/device-manager';
const id = 'a1111111-1111-4111-8111-111111111111';
const otherId = 'b1111111-1111-4111-8111-111111111111';
const rule = { id, paymentPlanId: otherId, daysBefore: 3, timeOfDay: '09:00', isEnabled: true };
test('reminder validation accepts 0/1/3/7/365 and exact local times; rejects future leads and malformed times', () => {
  for (const daysBefore of ['0', '1', '3', '7', '365']) expect(reminderFormSchema.safeParse({ daysBefore, timeOfDay: '23:59', isEnabled: true }).success).toBe(true);
  for (const daysBefore of ['-1', '366', '1.5', '']) expect(reminderFormSchema.safeParse({ daysBefore, timeOfDay: '09:00', isEnabled: true }).success).toBe(false);
  for (const timeOfDay of ['24:00', '9:00', '12:60']) expect(reminderFormSchema.safeParse({ daysBefore: '1', timeOfDay, isEnabled: true }).success).toBe(false);
});
test('settings accept IANA zones including DST zones and reject malformed zone/currency', () => {
  for (const zone of ['UTC', 'Europe/Istanbul', 'America/New_York']) expect(validTimeZone(zone)).toBe(true);
  expect(validTimeZone('invalid/zone')).toBe(false);
  expect(settingsFormSchema.safeParse({ languageCode: 'tr', defaultCurrencyCode: 'TRY', timeZoneId: 'badzone', notificationsEnabled: true }).success).toBe(false);
});
test('notification target uses strict typed IDs and ignores untrusted arbitrary URLs', () => {
  expect(notificationTargetSchema.parse({ notificationId: id, resourceId: otherId, kind: 'payment', url: 'https://untrusted.invalid' })).toEqual({ notificationId: id, resourceId: otherId, kind: 'payment' });
  for (const input of [{ kind: 'url', resourceId: id }, { kind: 'payment', resourceId: '../../users' }, { kind: 'subscription', resourceId: id, notificationId: 'bad' }]) expect(notificationTargetSchema.safeParse(input).success).toBe(false);
});
test('reminder CRUD sends existing DTO and leaves DST conversion to backend', async () => {
  const client = create(); const seen: { method?: string; path?: string; body: unknown }[] = [];
  client.defaults.adapter = async config => { seen.push({ method: config.method, path: config.url, body: config.data }); return { config, data: JSON.stringify(config.method === 'get' ? [rule] : config.method === 'delete' ? null : rule), status: 200, statusText: 'OK', headers: {} }; };
  const api = createNotificationApi(client); await api.reminders(otherId); await api.saveReminder(otherId, { daysBefore: 0, timeOfDay: '02:30', isEnabled: true }); await api.saveReminder(otherId, { daysBefore: 1, timeOfDay: '01:30', isEnabled: false }, id); await api.deleteReminder(otherId, id);
  expect(seen.map(x => x.method)).toEqual(['get', 'post', 'put', 'delete']); expect(JSON.parse(seen[1].body as string)).toEqual({ daysBefore: 0, timeOfDay: '02:30', isEnabled: true }); expect(seen[2].path).toBe(`/api/payments/${otherId}/reminders/${id}`);
});
test('stale session cannot write reminders', async () => {
  const client = create(); const adapter = jest.fn(async () => { throw new Error('unexpected'); }); client.defaults.adapter = adapter;
  await expect(createNotificationApi(client, { isCurrent: () => false }).saveReminder(otherId, { daysBefore: 1, timeOfDay: '09:00', isEnabled: true })).rejects.toMatchObject({ code: 'ERR_CANCELED' }); expect(adapter).not.toHaveBeenCalled();
});
function managerSetup() {
  let current: DeviceIdentity | null = { userId: 'owner', epoch: 1, accessToken: 'memory-only-owner' }; let binding: DeviceBinding | null = null; let token = 'ExpoPushToken[initial0123456789]';
  const register = jest.fn(async (_token: string, _id?: string) => ({ id })); const remove = jest.fn(async (_id: string) => {});
  const storage = { read: async () => binding, write: async (value: DeviceBinding) => { binding = value; }, clear: async () => { binding = null; } };
  const transport = jest.fn((_identity: DeviceIdentity) => ({ register, remove }));
  const manager = new DeviceManager({ identity: () => current, prepare: async () => {}, storage, transport, token: async () => token });
  return { manager, register, remove, storage, transport, owner: current!, binding: () => binding, change: (value: DeviceIdentity | null) => { current = value; }, rotate: () => { token = 'ExpoPushToken[rotated0123456789]'; } };
}
test('registration persists only owner/id; token rotation updates same device', async () => {
  const s = managerSetup(); await s.manager.sync(); s.rotate(); await s.manager.sync(); expect(s.register.mock.calls[1]).toEqual(['ExpoPushToken[rotated0123456789]', id]); expect(s.binding()).toEqual({ userId: 'owner', id }); expect(s.binding()).not.toHaveProperty('accessToken'); expect(s.binding()).not.toHaveProperty('pushToken');
});
test('logout removes device with captured old owner credential, not a new user bearer', async () => {
  const s = managerSetup(); await s.manager.sync(); s.change({ userId: 'new-owner', epoch: 2, accessToken: 'other-bearer' }); await s.manager.retire(s.owner); expect(s.transport).toHaveBeenLastCalledWith(s.owner); expect(s.remove).toHaveBeenCalledWith(id); expect(s.binding()).toBeNull();
});
test('offline cleanup keeps binding and prevents registering same installation to another user', async () => {
  const s = managerSetup(); await s.manager.sync(); s.remove.mockRejectedValueOnce(new Error('offline')); await expect(s.manager.retire(s.owner)).rejects.toThrow('offline'); expect(s.binding()).not.toBeNull(); s.change({ userId: 'new-owner', epoch: 2, accessToken: 'new' }); await expect(s.manager.sync()).rejects.toThrow('Önceki hesabın'); expect(s.register).toHaveBeenCalledTimes(1);
});
test('logout during in-flight registration rolls back device instead of leaking into next session', async () => {
  const s = managerSetup(); let finish!: (value: { id: string }) => void; s.register.mockImplementationOnce(() => new Promise(resolve => { finish = resolve; }));
  const registering = s.manager.sync(); for (let i = 0; i < 10 && !finish; i++) await Promise.resolve(); s.change(null); const logout = s.manager.retire(s.owner); finish({ id }); await expect(registering).rejects.toThrow('Oturum değişti'); await logout; expect(s.remove).toHaveBeenCalledWith(id); expect(s.binding()).toBeNull();
});
test('failed registration never claims a device was registered', async () => { const s = managerSetup(); s.register.mockRejectedValueOnce(new Error('offline')); await expect(s.manager.sync()).rejects.toThrow('offline'); expect(s.binding()).toBeNull(); });
test('failed refresh may retire device without waiting on its own registration queue', async () => {
  const owner = { userId: 'owner', epoch: 1, accessToken: 'old' }; const remove = jest.fn(async () => {}); let binding: DeviceBinding | null = { userId: owner.userId, id }; let manager: DeviceManager;
  manager = new DeviceManager({ identity: () => owner, prepare: async () => { await manager.retire(owner); throw new Error('refresh failed'); }, token: async () => 'unused', storage: { read: async () => binding, write: async value => { binding = value; }, clear: async () => { binding = null; } }, transport: () => ({ register: async () => ({ id }), remove }) });
  await expect(manager.sync()).rejects.toThrow('refresh failed'); expect(remove).toHaveBeenCalledWith(id); expect(binding).toBeNull();
});
