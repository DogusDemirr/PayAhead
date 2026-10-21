import { test, expect, jest, beforeEach, afterEach } from '@jest/globals';
import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import { tokenStorage } from '../src/services/token-storage';
jest.mock('expo-secure-store', () => ({ getItemAsync: jest.fn(), setItemAsync: jest.fn(), deleteItemAsync: jest.fn(), WHEN_UNLOCKED_THIS_DEVICE_ONLY: 6 }));
const original = Platform.OS;
const tokens = { accessToken: 'example-access', refreshToken: 'a'.repeat(64), accessTokenExpiresAtUtc: '2099-01-01T00:00:00Z', refreshTokenExpiresAtUtc: '2099-02-01T00:00:00Z' };
beforeEach(() => { Platform.OS = 'android'; jest.mocked(SecureStore.setItemAsync).mockResolvedValue(undefined); jest.mocked(SecureStore.deleteItemAsync).mockResolvedValue(undefined); });
afterEach(() => { Platform.OS = original; });
test('both tokens and expiration metadata are written as one secure native record', async () => {
  await tokenStorage.write(tokens);
  expect(SecureStore.setItemAsync).toHaveBeenCalledWith('payahead.session.v1', JSON.stringify(tokens), { keychainAccessible: 6 });
  jest.mocked(SecureStore.getItemAsync).mockResolvedValueOnce(JSON.stringify(tokens));
  expect(await tokenStorage.read()).toEqual(tokens);
});
test('malformed or incomplete secure records are removed', async () => {
  jest.mocked(SecureStore.getItemAsync).mockResolvedValueOnce('{broken');
  expect(await tokenStorage.read()).toBeNull(); expect(SecureStore.deleteItemAsync).toHaveBeenCalled();
});
test('delete failure falls back to overwriting the record with no credentials', async () => {
  jest.mocked(SecureStore.deleteItemAsync).mockRejectedValueOnce(new Error('delete'));
  await tokenStorage.clear(); expect(SecureStore.setItemAsync).toHaveBeenCalledWith('payahead.session.v1', 'null', expect.any(Object));
});
test('web does not fall back to insecure browser persistence', async () => {
  Platform.OS = 'web'; expect(await tokenStorage.read()).toBeNull();
  await expect(tokenStorage.write(tokens)).rejects.toThrow('Android veya iOS');
  expect(SecureStore.setItemAsync).not.toHaveBeenCalled();
});
