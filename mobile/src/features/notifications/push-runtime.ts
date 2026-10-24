import { Platform } from 'react-native';
import Constants from 'expo-constants';
import * as Device from 'expo-device';
import * as SecureStore from 'expo-secure-store';
import { create } from 'zustand';
import { session, setDeviceCleanup } from '../../services/auth-runtime';
import { createApiClient } from '../../services/api';
import { ApiError } from '../../services/api-error';
import { queryClient } from '../../services/query-client';
import { DeviceManager, bindingSchema } from './device-manager';
import { createNotificationApi } from './api';
import { notificationTargetSchema, type NotificationTarget } from './contracts';
export const channelId = 'payahead-reminders';
// Lazy native load keeps Expo Go/web from initializing an unsupported remote module.
// eslint-disable-next-line @typescript-eslint/no-require-imports -- native module must stay behind runtime availability checks
export function loadNotifications(): typeof import('expo-notifications') { return require('expo-notifications'); }
export const usePushState = create<{ permission: string; canAskAgain: boolean; registered: boolean; busy: boolean; error: string | null; pending: NotificationTarget | null }>(() => ({ permission: 'unknown', canAskAgain: true, registered: false, busy: false, error: null, pending: null }));
export function pushAvailability() {
  if (Platform.OS !== 'android' && Platform.OS !== 'ios') return 'Remote push için Android/iOS development build kullan.';
  if (Constants.appOwnership === 'expo' || Constants.executionEnvironment === 'storeClient') return 'Expo Go remote push için kullanılmaz. Development build gerekli.';
  if (!Device.isDevice) return 'Bu kabul akışı fiziksel cihaz gerektirir.';
  if (!Constants.expoConfig?.extra?.eas?.projectId && !Constants.easConfig?.projectId) return 'EAS projectId yapılandırılmamış. Push kurulumu belgesini izle.';
  return null;
}
const identity = () => { const state = session.getSnapshot(); const credential = session.credential(); return state.status === 'authenticated' && state.user && credential ? { userId: state.user.id, ...credential } : null; };
const storageKey = 'payahead.push-device.v1';
const optInKey = 'payahead.push-optin.v1';
const storage = {
  async read() { if (Platform.OS === 'web') return null; const raw = await SecureStore.getItemAsync(storageKey); if (!raw) return null; try { const result = bindingSchema.safeParse(JSON.parse(raw)); if (result.success) return result.data; } catch {} throw new Error('Cihaz kayıt bilgisi okunamadı; güvenli yeniden kayıt yapılamıyor.'); },
  async write(value: unknown) { await SecureStore.setItemAsync(storageKey, JSON.stringify(value), { keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY }); },
  async clear() { if (Platform.OS !== 'web') await SecureStore.deleteItemAsync(storageKey); },
};
export const devices = new DeviceManager({ identity, prepare: () => session.ensureFreshAccess(), storage,
  transport(owner) {
    const client = createApiClient(process.env.EXPO_PUBLIC_API_BASE_URL); client.defaults.headers.common.Authorization = `Bearer ${owner.accessToken}`;
    const api = createNotificationApi(client);
    return { register: (token, id) => api.registerDevice(token, Platform.OS === 'ios' ? 'ios' : 'android', id), remove: async id => { try { await api.deleteDevice(id); } catch (error) { if (!(error instanceof Error && 'status' in error && error.status === 404)) throw error; } } };
  },
  async token() { const n = loadNotifications(); let timer: ReturnType<typeof setTimeout> | undefined; try { const token = await Promise.race([n.getExpoPushTokenAsync({ projectId: Constants.expoConfig?.extra?.eas?.projectId ?? Constants.easConfig?.projectId }), new Promise<never>((_, reject) => { timer = setTimeout(() => reject(new Error('Push token alınırken zaman aşımı. Bağlantıyla yeniden dene.')), 20000); })]); return token.data; } finally { if (timer) clearTimeout(timer); } },
});
setDeviceCleanup(async owner => { usePushState.setState({ registered: false, pending: null }); await devices.retire(owner); });
export async function refreshPushPermission() {
  if (pushAvailability()) return;
  const n = loadNotifications(); const p = await n.getPermissionsAsync(); usePushState.setState({ permission: p.status, canAskAgain: p.canAskAgain }); return p;
}
// Only an explicit user action may show the runtime permission prompt.
export async function connectPush(askPermission = false) {
  if (usePushState.getState().busy) return;
  usePushState.setState({ busy: true, error: null });
  try {
    const unavailable = pushAvailability(); if (unavailable) throw new Error(unavailable);
    const owner = identity(); if (!owner) return;
    if (!askPermission && await SecureStore.getItemAsync(optInKey) !== owner.userId) return;
    const n = loadNotifications();
    if (Platform.OS === 'android') await n.setNotificationChannelAsync(channelId, { name: 'Ödeme ve deneme hatırlatmaları', importance: n.AndroidImportance.DEFAULT, lockscreenVisibility: n.AndroidNotificationVisibility.PRIVATE });
    let p = await refreshPushPermission();
    if (!p?.granted && askPermission && p?.canAskAgain) { p = await n.requestPermissionsAsync(); usePushState.setState({ permission: p.status, canAskAgain: p.canAskAgain }); }
    if (!p?.granted) { await devices.retire(owner); usePushState.setState({ registered: false }); return; }
    if (askPermission) await SecureStore.setItemAsync(optInKey, owner.userId);
    await devices.sync(); usePushState.setState({ registered: true });
    if (identity()?.epoch === owner.epoch) await queryClient.invalidateQueries({ queryKey: ['finance', owner.userId, owner.epoch, 'notifications', 'devices'] });
  } catch (error) { const safe = error instanceof ApiError || error instanceof Error && ['Remote push', 'Expo Go', 'Bu kabul', 'EAS projectId', 'Önceki hesabın', 'Cihaz kayıt bilgisi', 'Oturum değişti', 'Push token alınırken'].some(prefix => error.message.startsWith(prefix)); usePushState.setState({ registered: false, error: safe ? (error as Error).message : 'Cihaz kaydı yapılamadı. Bağlantını ve FCM/EAS push yapılandırmasını kontrol et.' }); }
  finally { usePushState.setState({ busy: false }); }
}
export async function disconnectPush() {
  const owner = identity(); if (!owner) return;
  await SecureStore.deleteItemAsync(optInKey);
  await devices.retire(owner); usePushState.setState({ registered: false }); await queryClient.invalidateQueries({ queryKey: ['finance', owner.userId, owner.epoch, 'notifications', 'devices'] });
}
export async function acknowledgeRemovedDevice(id: string) {
  const owner = identity(); const binding = await storage.read();
  if (owner && binding?.userId === owner.userId && binding.id === id) await disconnectPush();
}
export function receiveTarget(data: unknown) { const parsed = notificationTargetSchema.safeParse(data); if (parsed.success) usePushState.setState({ pending: parsed.data }); }
