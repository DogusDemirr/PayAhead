import { beforeEach, expect, jest, test } from '@jest/globals';
import { act, fireEvent, render, renderHook, screen } from '@testing-library/react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import * as ImagePicker from 'expo-image-picker';
import { ImageManipulator } from 'expo-image-manipulator';
import { chooseAvatarPhoto, ProfileAvatar, useAvatarMutation } from '../src/features/account/avatar';
import { apiClient, currentUserKey, session } from '../src/services/auth-runtime';
import { useSessionStore } from '../src/store/session';
jest.mock('expo-image-picker', () => ({ launchImageLibraryAsync: jest.fn() }));
jest.mock('expo-image-manipulator', () => ({ SaveFormat: { JPEG: 'jpeg' }, ImageManipulator: { manipulate: jest.fn() } }));
jest.mock('../src/services/auth-runtime', () => ({ apiClient: require('axios').create({ baseURL: 'http://localhost:5094' }), currentUserKey: () => ['current-user', 1], session: { credential: jest.fn(() => ({ epoch: 1, accessToken: 'fixture-only' })), ensureFreshAccess: async () => undefined, updateUser: jest.fn() } }));
const user = { id: 'aaaaaaaa-aaaa-4aaa-baaa-aaaaaaaaaaaa', email: 'a@example.invalid', displayName: 'Ada Lovelace' };
const profile = { ...user, hasAvatar: true, rowVersion: 'AAAAAAAAAAI=' };
beforeEach(() => { jest.clearAllMocks(); jest.mocked(session.credential).mockReturnValue({ epoch: 1, accessToken: 'fixture-only' }); useSessionStore.setState({ status: 'authenticated', user }); });
test('picker cancellation preserves the current image without manipulation', async () => {
  jest.mocked(ImagePicker.launchImageLibraryAsync).mockResolvedValue({ canceled: true, assets: null }); expect(await chooseAvatarPhoto()).toBeNull(); expect(ImageManipulator.manipulate).not.toHaveBeenCalled();
});
test('permission rejection produces a safe localized code', async () => {
  jest.mocked(ImagePicker.launchImageLibraryAsync).mockRejectedValue({ code: 'E_PERMISSION_DENIED' }); await expect(chooseAvatarPhoto()).rejects.toMatchObject({ code: 'avatar_permission' });
});
test('selected image is square cropped and optimized to 512 JPEG without EXIF request', async () => {
  jest.mocked(ImagePicker.launchImageLibraryAsync).mockResolvedValue({ canceled: false, assets: [{ uri: 'file:///source.jpg', width: 2000, height: 1000, type: 'image' }] });
  const image = { saveAsync: jest.fn(async () => ({ uri: 'file:///optimized.jpg', width: 512, height: 512 })), release: jest.fn() };
  const context = { crop: jest.fn(), resize: jest.fn(), renderAsync: jest.fn(async () => image), release: jest.fn() }; context.crop.mockReturnValue(context); context.resize.mockReturnValue(context);
  jest.mocked(ImageManipulator.manipulate).mockReturnValue(context as unknown as ReturnType<typeof ImageManipulator.manipulate>);
  expect((await chooseAvatarPhoto())?.uri).toBe('file:///optimized.jpg'); expect(context.crop).toHaveBeenCalledWith({ originX: 500, originY: 0, width: 1000, height: 1000 }); expect(context.resize).toHaveBeenCalledWith({ width: 512, height: 512 });
  expect(image.saveAsync).toHaveBeenCalledWith({ format: 'jpeg', compress: 0.85, base64: false }); expect(image.release).toHaveBeenCalled(); expect(context.release).toHaveBeenCalled();
  expect(ImagePicker.launchImageLibraryAsync).toHaveBeenCalledWith(expect.objectContaining({ allowsEditing: true, aspect: [1, 1], exif: false }));
});
test('private avatar uses authenticated own endpoint and falls back on image error', async () => {
  await render(<ProfileAvatar id={user.id} name={user.displayName} hasAvatar version={profile.rowVersion} />);
  const photo = screen.getByRole('image', { name: user.displayName }); expect(photo).toBeTruthy();
  const image = screen.getByTestId('profile-avatar-image'); expect(image.props.source.headers.Authorization).toBe('Bearer fixture-only'); expect(image.props.source.uri).not.toContain('fixture-only');
  await fireEvent(image, 'error'); expect(screen.queryByTestId('profile-avatar-image')).toBeNull();
  expect(screen.getByText('AL')).toBeTruthy();
});
test.each(['upload', 'remove'] as const)('%s commits only a confirmed server profile and leaves other accounts intact', async kind => {
  const cache = new QueryClient(); const key = ['finance', user.id, 1, 'profile']; cache.setQueryData(key, profile); const foreign = ['finance', 'other', 2, 'profile']; cache.setQueryData(foreign, profile);
  let header: unknown; apiClient.defaults.adapter = async config => { header = config.headers['If-Match']; return { data: { ...profile, hasAvatar: kind === 'upload' }, status: 200, statusText: 'OK', headers: {}, config }; };
  const view = await renderHook(() => useAvatarMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  await act(async () => { await view.result.current.mutateAsync({ kind, rowVersion: profile.rowVersion, uri: 'file:///optimized.jpg' }); });
  expect(header).toBe('"' + profile.rowVersion + '"'); expect(cache.getQueryData(key)).toMatchObject({ hasAvatar: kind === 'upload' }); expect(cache.getQueryData(currentUserKey())).toMatchObject({ hasAvatar: kind === 'upload' }); expect(cache.getQueryData(foreign)).toEqual(profile); await view.unmount(); cache.clear();
});
test('upload failure preserves the previously confirmed photo', async () => {
  const cache = new QueryClient(); const key = ['finance', user.id, 1, 'profile']; cache.setQueryData(key, profile);
  apiClient.defaults.adapter = async () => { throw new Error('offline'); };
  const view = await renderHook(() => useAvatarMutation(), { wrapper: ({ children }: { children: ReactNode }) => <QueryClientProvider client={cache}>{children}</QueryClientProvider> });
  await act(async () => { await expect(view.result.current.mutateAsync({ kind: 'upload', rowVersion: profile.rowVersion, uri: 'file:///optimized.jpg' })).rejects.toThrow(); });
  expect(cache.getQueryData(key)).toEqual(profile); expect(session.updateUser).not.toHaveBeenCalled(); await view.unmount(); cache.clear();
});
