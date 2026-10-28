import { useRef, useState } from 'react';
import { ActivityIndicator, Image, Platform, Pressable, View } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { ImageManipulator, SaveFormat } from 'expo-image-manipulator';
import { Camera } from 'lucide-react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { CanceledError } from 'axios';
import { AppButton, AppText } from '../../components/ui';
import { DialogFrame, useAppDialog } from '../../components/dialogs';
import { currentLocale, t, useLocaleRevision } from '../../i18n/runtime';
import { apiClient, currentUserKey, session } from '../../services/auth-runtime';
import { ApiError } from '../../services/api-error';
import { useSessionStore } from '../../store/session';
import { useTheme } from '../../theme/use-theme';
import { useFinanceScope } from '../payments/queries';
import { profileSchema, type Profile } from './queries';

export async function chooseAvatarPhoto() {
  // SDK 57's system photo picker needs no broad library permission. Camera/mic are disabled.
  let result: ImagePicker.ImagePickerResult;
  try { result = await ImagePicker.launchImageLibraryAsync({ mediaTypes: ['images'], allowsEditing: true, allowsMultipleSelection: false, aspect: [1, 1], quality: 1, exif: false, base64: false }); }
  catch (e) { throw new ApiError('', undefined, undefined, undefined, 'http', /permission|denied/i.test(String((e as { code?: string })?.code)) ? 'avatar_permission' : 'avatar_failed'); }
  if (result.canceled) return null;
  const asset = result.assets[0];
  if (!asset || asset.type === 'video' || !Number.isFinite(asset.width) || !Number.isFinite(asset.height) || asset.width < 1 || asset.height < 1) throw new ApiError('', undefined, undefined, undefined, 'http', 'avatar_invalid');
  const side = Math.min(asset.width, asset.height); const context = ImageManipulator.manipulate(asset.uri);
  try {
    const image = await context.crop({ originX: Math.floor((asset.width - side) / 2), originY: Math.floor((asset.height - side) / 2), width: side, height: side }).resize({ width: 512, height: 512 }).renderAsync();
    try { return await image.saveAsync({ format: SaveFormat.JPEG, compress: 0.85, base64: false }); }
    finally { image.release(); }
  } finally { context.release(); }
}
export function useAvatarMutation() {
  const scope = useFinanceScope(); const cache = useQueryClient();
  return useMutation({ retry: false, gcTime: 0, mutationFn: async (input: { kind: 'upload' | 'remove'; rowVersion: string; uri?: string }) => {
    const check = () => { if (!scope.context.isCurrent()) throw new CanceledError(); };
    check(); await scope.context.prepareWrite(); check();
    const config = { headers: { 'If-Match': `"${input.rowVersion}"` }, ...{ sessionEpoch: scope.context.epoch } };
    let response;
    if (input.kind === 'remove') response = await apiClient.delete('/api/users/me/avatar', config);
    else {
      if (!input.uri || !/^(file:|content:|blob:|data:image\/)/.test(input.uri)) throw new ApiError('', undefined, undefined, undefined, 'http', 'avatar_invalid');
      const body = new FormData();
      if (Platform.OS === 'web') body.append('image', await (await fetch(input.uri)).blob(), 'avatar.jpg');
      else body.append('image', { uri: input.uri, type: 'image/jpeg', name: 'avatar.jpg' } as unknown as Blob);
      response = await apiClient.put('/api/users/me/avatar', body, config);
    }
    check(); const profile = profileSchema.parse(response.data);
    if (profile.id !== scope.key[1]) throw new CanceledError();
    const user = { id: profile.id, email: profile.email, displayName: profile.displayName, hasAvatar: profile.hasAvatar, avatarVersion: profile.rowVersion };
    session.updateUser(user, scope.context.epoch!); cache.setQueryData(currentUserKey(), user); cache.setQueryData([...scope.key, 'profile'], profile);
  }, onError: () => { if (scope.context.isCurrent()) void cache.invalidateQueries({ queryKey: [...scope.key, 'profile'] }); } });
}
export function ProfileAvatar({ id, name, hasAvatar = false, version = '', size = 64 }: { id: string; name: string; hasAvatar?: boolean; version?: string; size?: number }) {
  useLocaleRevision(); const { colors } = useTheme(); const owner = useSessionStore(s => s.user?.id);
  const credential = hasAvatar && owner === id ? session.credential() : null;
  let uri: string | undefined;
  try { const base = new URL(apiClient.defaults.baseURL!); if (credential && (__DEV__ || base.protocol === 'https:') && ['http:', 'https:'].includes(base.protocol) && !base.username && !base.password) { const url = new URL('/api/users/me/avatar', base); url.searchParams.set('v', version); url.searchParams.set('account', id); uri = url.toString(); } } catch { /* initials while API configuration is unavailable */ }
  const initials = name.trim().split(/\s+/).filter(Boolean).slice(0, 2).map(x => [...x][0]).join('').toLocaleUpperCase(currentLocale());
  return <View accessible accessibilityRole="image" accessibilityLabel={name} style={{ width: size, height: size, borderRadius: size / 2, overflow: 'hidden', backgroundColor: colors.accentSoft, alignItems: 'center', justifyContent: 'center' }}><AppText variant="title" tone="accentText">{initials}</AppText>{uri && credential && <AvatarImage key={uri} uri={uri} token={credential.accessToken} size={size} />}</View>;
}
function AvatarImage({ uri, token, size }: { uri: string; token: string; size: number }) {
  const [failed, setFailed] = useState(false); const [loading, setLoading] = useState(true); const { colors } = useTheme();
  if (failed) return null;
  return <View style={{ position: 'absolute', inset: 0 }}><Image testID="profile-avatar-image" source={{ uri, headers: { Authorization: `Bearer ${token}` }, cache: 'reload' }} style={{ width: size, height: size }} resizeMode="cover" onLoadEnd={() => setLoading(false)} onError={() => setFailed(true)} />{loading && <ActivityIndicator color={colors.accentText} style={{ position: 'absolute', inset: 0 }} />}</View>;
}
export function AvatarControl({ profile, onSuccess }: { profile: Profile; onSuccess?(): void }) {
  useLocaleRevision(); const { colors } = useTheme(); const dialog = useAppDialog(); const mutation = useAvatarMutation();
  const [open, setOpen] = useState(false); const [busy, setBusy] = useState(false); const gate = useRef(false);
  async function action(remove: boolean) {
    if (gate.current) return; gate.current = true; setOpen(false); setBusy(true);
    try {
      const image = remove ? null : await chooseAvatarPhoto(); if (!remove && !image) return;
      await mutation.mutateAsync({ kind: remove ? 'remove' : 'upload', rowVersion: profile.rowVersion, ...(image ? { uri: image.uri } : {}) }); onSuccess?.();
    } catch (e) { dialog.error(e); }
    finally { gate.current = false; setBusy(false); mutation.reset(); }
  }
  return <><Pressable accessibilityRole="button" accessibilityLabel={t('settings:avatar.change')} accessibilityState={{ busy, disabled: busy }} disabled={busy} onPress={() => setOpen(true)}><ProfileAvatar id={profile.id} name={profile.displayName} hasAvatar={profile.hasAvatar} version={profile.rowVersion} /><View style={{ position: 'absolute', bottom: -2, end: -2, borderRadius: 14, backgroundColor: colors.accentText, padding: 6 }}>{busy ? <ActivityIndicator accessibilityLabel={t('settings:avatar.uploading')} color={colors.white} size="small" /> : <Camera size={17} color={colors.white} />}</View></Pressable>
    <DialogFrame visible={open} title={t('settings:avatar.change')} onClose={() => setOpen(false)}><AppButton title={t('settings:avatar.pick')} onPress={() => { void action(false); }} />{profile.hasAvatar && <AppButton variant="destructive" title={t('settings:avatar.remove')} onPress={() => { void action(true); }} />}<AppButton variant="ghost" title={t('common:cancel')} onPress={() => setOpen(false)} /></DialogFrame>{dialog.dialog}</>;
}
