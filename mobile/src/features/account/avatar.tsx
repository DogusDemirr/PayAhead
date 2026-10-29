import { useRef, useState } from 'react';
import { ActivityIndicator, Image, Platform, Pressable, View } from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { ImageManipulator, SaveFormat } from 'expo-image-manipulator';
import { Camera } from 'lucide-react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CanceledError, isCancel } from 'axios';
import { AppButton, AppText } from '../../components/ui';
import { DialogFrame, useAppDialog } from '../../components/dialogs';
import { currentLocale, t, useLocaleRevision } from '../../i18n/runtime';
import { apiClient, currentUserKey, session } from '../../services/auth-runtime';
import { ApiError } from '../../services/api-error';
import { useSessionStore } from '../../store/session';
import { useTheme } from '../../theme/use-theme';
import { useFinanceScope } from '../payments/queries';
import { profileSchema, type Profile } from './queries';
import { classifyAvatarFailure, type AvatarStage } from '../../services/avatar-error';
import { avatarSourceOptions } from './avatar-source';

export async function chooseAvatarPhoto() {
  // SDK 57's system photo picker needs no broad library permission. Camera/mic are disabled.
  let result: ImagePicker.ImagePickerResult;
  try { result = await ImagePicker.launchImageLibraryAsync({ mediaTypes: ['images'], allowsEditing: true, allowsMultipleSelection: false, aspect: [1, 1], quality: 1, exif: false, base64: false }); }
  catch (e) { if (isCancel(e) || /cancel/i.test(String((e as { code?: string })?.code))) return null; throw classifyAvatarFailure(new ApiError('', undefined, undefined, undefined, 'http', /permission|denied/i.test(String((e as { code?: string })?.code)) ? 'avatar_permission' : 'avatar_failed'), 'selection'); }
  if (result.canceled) return null;
  const asset = result.assets[0];
  if (!asset || asset.type === 'video' || !Number.isFinite(asset.width) || !Number.isFinite(asset.height) || asset.width < 1 || asset.height < 1) throw new ApiError('', undefined, undefined, undefined, 'http', 'avatar_invalid');
  const side = Math.min(asset.width, asset.height); const context = ImageManipulator.manipulate(asset.uri);
  try {
    const image = await context.crop({ originX: Math.floor((asset.width - side) / 2), originY: Math.floor((asset.height - side) / 2), width: side, height: side }).resize({ width: 512, height: 512 }).renderAsync();
    try { return await image.saveAsync({ format: SaveFormat.JPEG, compress: 0.85, base64: false }); }
    finally { image.release(); }
  } catch (e) { throw classifyAvatarFailure(e, 'preparation'); }
  finally { context.release(); }
}
export function useAvatarMutation() {
  const scope = useFinanceScope(); const cache = useQueryClient();
  return useMutation({ retry: false, gcTime: 0, mutationFn: async (input: { kind: 'upload' | 'remove'; rowVersion: string; uri?: string }) => {
    let stage: AvatarStage = 'request';
    try {
    const check = () => { if (!scope.context.isCurrent()) throw new CanceledError(); };
    check(); await scope.context.prepareWrite(); check();
    // Native URI parts belong to RN's XHR networking, not Expo fetch's Blob-only encoder.
    // The adapter supplies the multipart boundary; never set a bare multipart Content-Type.
    const config = { headers: { 'If-Match': `"${input.rowVersion}"` }, sessionEpoch: scope.context.epoch };
    let response;
    if (input.kind === 'remove') response = await apiClient.delete('/api/users/me/avatar', config);
    else {
      stage = 'multipart';
      if (!input.uri || !/^(file:|content:|blob:|data:image\/)/.test(input.uri)) throw new ApiError('', undefined, undefined, undefined, 'http', 'avatar_invalid');
      const body = new FormData();
      if (Platform.OS === 'web') body.append('image', await (await fetch(input.uri)).blob(), 'avatar.jpg');
      else body.append('image', { uri: input.uri, type: 'image/jpeg', name: 'avatar.jpg' } as unknown as Blob);
      stage = 'request'; response = await apiClient.put('/api/users/me/avatar', body, { ...config, ...(Platform.OS !== 'web' ? { adapter: 'xhr' as const } : {}) });
    }
    stage = 'response'; check(); const parsed = profileSchema.safeParse(response.data);
    if (!parsed.success) throw new ApiError('', response.status, undefined, undefined, 'http', 'avatar_unconfirmed');
    const profile = parsed.data;
    if (profile.id !== scope.key[1]) throw new CanceledError();
    if (profile.hasAvatar !== (input.kind === 'upload')) throw new ApiError('', response.status, undefined, undefined, 'http', 'avatar_unconfirmed');
    const user = { id: profile.id, email: profile.email, displayName: profile.displayName, hasAvatar: profile.hasAvatar, avatarVersion: profile.rowVersion };
    session.updateUser(user, scope.context.epoch!); cache.setQueryData(currentUserKey(), user); cache.setQueryData([...scope.key, 'profile'], profile);
    if (input.kind === 'upload') {
      try { await cache.fetchQuery(avatarSourceOptions(apiClient, { id: profile.id, epoch: scope.context.epoch!, version: profile.rowVersion, isCurrent: scope.context.isCurrent, prepare: scope.context.prepareWrite })); check(); }
      catch (error) { if (isCancel(error) || error instanceof ApiError && (error.status === 401 || error.kind === 'session')) throw error; throw new ApiError('', 200, undefined, undefined, 'http', 'avatar_unconfirmed'); }
    } else cache.removeQueries({ queryKey: [...scope.key, 'avatar'] });
    } catch (error) {
      if (error instanceof ApiError && error.status === 409 && error.code === 'avatar_changed' && scope.context.isCurrent()) {
        // Refresh the shared profile safely, but never replay a write with a new version automatically.
        try {
          const fresh = profileSchema.parse((await apiClient.get('/api/users/me/profile', { headers: { Accept: 'application/json' }, ...{ sessionEpoch: scope.context.epoch } })).data);
          if (scope.context.isCurrent() && fresh.id === scope.key[1]) {
            const user = { id: fresh.id, email: fresh.email, displayName: fresh.displayName, hasAvatar: fresh.hasAvatar, avatarVersion: fresh.rowVersion };
            session.updateUser(user, scope.context.epoch!); cache.setQueryData(currentUserKey(), user); cache.setQueryData([...scope.key, 'profile'], fresh);
          }
        } catch { /* Original failure remains visible; no write is retried. */ }
      }
      throw classifyAvatarFailure(error, stage);
    }
  }, onError: (error) => { if (!isCancel(error) && scope.context.isCurrent()) void cache.invalidateQueries({ queryKey: [...scope.key, 'profile'] }); } });
}
export function ProfileAvatar({ id, name, hasAvatar = false, version = '', size = 64 }: { id: string; name: string; hasAvatar?: boolean; version?: string; size?: number }) {
  useLocaleRevision(); const { colors } = useTheme(); const owner = useSessionStore(s => s.user?.id);
  const credential = hasAvatar && owner === id ? session.credential() : null;
  const initials = name.trim().split(/\s+/).filter(Boolean).slice(0, 2).map(x => [...x][0]).join('').toLocaleUpperCase(currentLocale());
  return <View accessible accessibilityRole="image" accessibilityLabel={name} style={{ width: size, height: size, borderRadius: size / 2, overflow: 'hidden', backgroundColor: colors.accentSoft, alignItems: 'center', justifyContent: 'center' }}><AppText variant="title" tone="accentText">{initials}</AppText>{credential && <AuthenticatedAvatar key={`${id}:${credential.epoch}:${version}`} id={id} epoch={credential.epoch} version={version} size={size} />}</View>;
}
function AuthenticatedAvatar({ id, epoch, version, size }: { id: string; epoch: number; version: string; size: number }) {
  const query = useQuery(avatarSourceOptions(apiClient, { id, epoch, version, prepare: () => session.ensureFreshAccess(), isCurrent: () => session.credential()?.epoch === epoch && useSessionStore.getState().user?.id === id }));
  const { colors } = useTheme();
  return query.data ? <AvatarImage uri={query.data} size={size} /> : query.isFetching ? <ActivityIndicator testID="profile-avatar-loading" color={colors.accentText} style={{ position: 'absolute', top: 0, left: 0, width: size, height: size, zIndex: 2 }} /> : null;
}
function AvatarImage({ uri, size }: { uri: string; size: number }) {
  const [result, setResult] = useState<{ uri: string; phase: 'loading' | 'loaded' | 'error' }>({ uri, phase: 'loading' }); const { colors } = useTheme();
  const current = result.uri === uri;
  const loading = !current || result.phase === 'loading';
  if (current && result.phase === 'error') return null;
  // Keep the actual bitmap explicitly positioned/sized above the initials. Do not
  // depend on a layout-only absolute wrapper's shorthand inset on Android Fabric.
  return <><Image testID="profile-avatar-image" source={{ uri }} style={{ position: 'absolute', top: 0, left: 0, width: size, height: size, zIndex: 1 }} resizeMode="cover" onLoad={() => setResult({ uri, phase: 'loaded' })} onError={() => { setResult({ uri, phase: 'error' }); if (__DEV__) console.info('avatar.display', { outcome: 'image_error' }); }} />{loading && <ActivityIndicator testID="profile-avatar-loading" color={colors.accentText} style={{ position: 'absolute', top: 0, left: 0, width: size, height: size, zIndex: 2 }} />}</>;
}
export function AvatarControl({ profile, onSuccess, onStart }: { profile: Profile; onSuccess?(): void; onStart?(): void }) {
  useLocaleRevision(); const { colors } = useTheme(); const dialog = useAppDialog(); const mutation = useAvatarMutation();
  const [open, setOpen] = useState(false); const [busy, setBusy] = useState(false); const gate = useRef(false);
  async function action(remove: boolean) {
    if (gate.current) return; gate.current = true; onStart?.(); setOpen(false); setBusy(true);
    try {
      const image = remove ? null : await chooseAvatarPhoto(); if (!remove && !image) return;
      await mutation.mutateAsync({ kind: remove ? 'remove' : 'upload', rowVersion: profile.rowVersion, ...(image ? { uri: image.uri } : {}) }); onSuccess?.();
    } catch (e) { if (!isCancel(e)) dialog.error(classifyAvatarFailure(e, 'preparation')); }
    finally { gate.current = false; setBusy(false); mutation.reset(); }
  }
  return <><Pressable accessibilityRole="button" accessibilityLabel={t('settings:avatar.change')} accessibilityState={{ busy, disabled: busy }} disabled={busy} onPress={() => setOpen(true)}><ProfileAvatar id={profile.id} name={profile.displayName} hasAvatar={profile.hasAvatar} version={profile.rowVersion} /><View style={{ position: 'absolute', bottom: -2, end: -2, borderRadius: 14, backgroundColor: colors.accentText, padding: 6 }}>{busy ? <ActivityIndicator accessibilityLabel={t('settings:avatar.uploading')} color={colors.white} size="small" /> : <Camera size={17} color={colors.white} />}</View></Pressable>
    <DialogFrame visible={open} title={t('settings:avatar.change')} onClose={() => setOpen(false)}><AppButton title={t('settings:avatar.pick')} onPress={() => { void action(false); }} />{profile.hasAvatar && <AppButton variant="destructive" title={t('settings:avatar.remove')} onPress={() => { void action(true); }} />}<AppButton variant="ghost" title={t('common:cancel')} onPress={() => setOpen(false)} /></DialogFrame>{dialog.dialog}</>;
}
