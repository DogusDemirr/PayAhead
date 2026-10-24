import { beforeEach, test, expect, jest } from '@jest/globals';
import { render, act, waitFor } from '@testing-library/react-native';
import { router } from 'expo-router';
import * as Notifications from 'expo-notifications';
import { PushBootstrap } from '../src/features/notifications/PushBootstrap';
import { usePushState, connectPush } from '../src/features/notifications/push-runtime';
import { useSessionStore } from '../src/store/session';
import { apiClient } from '../src/services/auth-runtime';
jest.mock('../src/services/auth-runtime', () => ({ apiClient: { get: jest.fn() }, session: { credential: () => ({ epoch: 1, accessToken: 'memory' }), getSnapshot: () => ({ status: 'authenticated', user: { id: 'owner' } }) } }));
jest.mock('expo-notifications', () => ({ DEFAULT_ACTION_IDENTIFIER: 'default', setNotificationHandler: jest.fn(), addNotificationResponseReceivedListener: jest.fn(() => ({ remove: jest.fn() })), addPushTokenListener: jest.fn(() => ({ remove: jest.fn() })), addNotificationReceivedListener: jest.fn(() => ({ remove: jest.fn() })), getLastNotificationResponseAsync: jest.fn(async () => null), clearLastNotificationResponseAsync: jest.fn(async () => {}) }));
jest.mock('../src/features/notifications/push-runtime', () => ({ pushAvailability: () => null, connectPush: jest.fn(async () => {}), loadNotifications: () => require('expo-notifications'), usePushState: require('zustand').create(() => ({ pending: null, registered: false })), receiveTarget: (data: unknown) => { const parsed = require('../src/features/notifications/contracts').notificationTargetSchema.safeParse(data); if (parsed.success) require('../src/features/notifications/push-runtime').usePushState.setState({ pending: parsed.data }); } }));
const target = { notificationId: 'a1111111-1111-4111-8111-111111111111', kind: 'payment' as const, resourceId: 'b1111111-1111-4111-8111-111111111111' };
beforeEach(() => { usePushState.setState({ pending: null }); useSessionStore.setState({ status: 'authenticated', user: { id: 'owner', email: 'owner@example.invalid', displayName: 'Owner' }, notice: null }); jest.mocked(apiClient.get).mockResolvedValue({ data: {} }); });
test('tap verifies ownership through real API path before payment navigation', async () => {
  usePushState.setState({ pending: target }); await render(<PushBootstrap />); await waitFor(() => expect(router.push).toHaveBeenCalledWith({ pathname: '/payment', params: { id: target.resourceId } })); expect(apiClient.get).toHaveBeenCalledWith(`/api/payments/${target.resourceId}`, expect.objectContaining({ sessionEpoch: 1 }));
});
test('anonymous tap waits for login, then validates subscription using the new account', async () => {
  useSessionStore.setState({ status: 'anonymous', user: null }); usePushState.setState({ pending: { ...target, kind: 'subscription' } }); await render(<PushBootstrap />); expect(router.replace).toHaveBeenCalledWith('/(auth)/login'); expect(apiClient.get).not.toHaveBeenCalled();
  await act(async () => { useSessionStore.setState({ status: 'authenticated', user: { id: 'new-owner', email: 'other@example.invalid', displayName: 'Other' } }); }); await waitFor(() => expect(router.push).toHaveBeenCalledWith({ pathname: '/subscription', params: { id: target.resourceId } })); expect(apiClient.get).toHaveBeenCalledWith(`/api/subscriptions/${target.resourceId}`, expect.anything());
});
test('deleted or foreign resource never opens detail and gives a neutral message', async () => {
  jest.mocked(apiClient.get).mockRejectedValueOnce({ status: 404 }); usePushState.setState({ pending: target }); await render(<PushBootstrap />); await waitFor(() => expect(router.push).toHaveBeenCalledWith({ pathname: '/information', params: expect.objectContaining({ message: 'Kayıt kaldırılmış olabilir veya bu hesapla erişilemiyor.' }) })); expect(usePushState.getState().pending).toBeNull();
});
test('offline ownership lookup cannot navigate to an unverified resource', async () => {
  jest.mocked(apiClient.get).mockRejectedValueOnce({ kind: 'network' }); usePushState.setState({ pending: target }); await render(<PushBootstrap />); await waitFor(() => expect(router.push).toHaveBeenCalledWith({ pathname: '/information', params: expect.objectContaining({ message: expect.stringContaining('Bağlantını kontrol et') }) }));
});
test('native token event re-fetches Expo token; malformed notification does not navigate', async () => {
  await render(<PushBootstrap />); await waitFor(() => expect(Notifications.addPushTokenListener).toHaveBeenCalled()); const before = jest.mocked(connectPush).mock.calls.length;
  await act(async () => { jest.mocked(Notifications.addPushTokenListener).mock.calls[0][0]({ type: 'android', data: 'native-not-expo' }); }); expect(jest.mocked(connectPush).mock.calls.length).toBe(before + 1);
  await act(async () => { jest.mocked(Notifications.addNotificationResponseReceivedListener).mock.calls[0][0]({ actionIdentifier: 'default', notification: { request: { content: { data: { kind: 'payment', resourceId: 'bad' } } } } } as unknown as Notifications.NotificationResponse); }); expect(router.push).not.toHaveBeenCalled();
});
