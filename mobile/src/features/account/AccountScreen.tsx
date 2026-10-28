import { AvatarControl } from './avatar';
import { SuccessFeedback, useAppDialog } from '../../components/dialogs';
import { useRef, useState } from 'react';
import { Linking, Platform, View } from 'react-native';
import { router } from 'expo-router';
import { Bell, Globe, Clock } from 'lucide-react-native';
import { AppScreen, AppHeader, AppCard, AppText, AppInput, AppButton, SectionHeader, SettingRow, StatusBadge, LoadingSkeleton, styles } from '../../components/ui';
import { t, currentLocale, useLocaleRevision } from '../../i18n/runtime';
import { useTheme } from '../../theme/use-theme';
import { ApiError } from '../../services/api-error';
import { session } from '../../services/auth-runtime';
import { useEntitlements } from '../payments/queries';
import { FinanceError } from '../payments/components';
import { useNotificationSettings } from '../notifications/queries';
import { useAccountMutation, useProfile, type Profile } from './queries';

const key = (name: string) => 'settings:profile.' + name;
export default function AccountScreen() {

  useLocaleRevision(); const profile = useProfile(); const membership = useEntitlements(); const settings = useNotificationSettings(); const { colors } = useTheme();
  const [deleting, setDeleting] = useState(false); const [linkError, setLinkError] = useState(false); const [saved, setSaved] = useState(false); const [photoSaved, setPhotoSaved] = useState(false);
  return <AppScreen refreshing={profile.isFetching} onRefresh={() => { void profile.refetch(); void membership.refetch(); void settings.refetch(); }}>
    <AppHeader title={t(key('accountTitle'))} back />
    {profile.isError && <FinanceError error={profile.error} retry={() => { void profile.refetch(); }} />}
    {!profile.data ? !profile.isError && <LoadingSkeleton /> : <>
      <AppCard style={{ backgroundColor: colors.navy }}><View style={[styles.row, { gap: 16 }]}><AvatarControl profile={profile.data} onSuccess={() => setPhotoSaved(true)} /><View style={{ flex: 1 }}><AppText variant="section" tone="onNavy">{profile.data.displayName}</AppText><AppText variant="caption" tone="onNavyMuted">{profile.data.email}</AppText></View></View><View style={{ marginTop: 16 }}><StatusBadge label={membership.data && !membership.isError ? t(key(membership.data.isPremium ? 'premium' : 'free')) : t(key('unknownMembership'))} tone={membership.data?.isPremium && !membership.isError ? 'accent' : 'muted'} /></View></AppCard>
      {photoSaved && <SuccessFeedback message={t('settings:avatar.updated')} />}<SectionHeader title={t(key('personal'))} />{saved && <AppText tone="success" accessibilityRole="alert">{t(key('profileSaved'))}</AppText>}<ProfileForm key={profile.data.rowVersion} profile={profile.data} onSaved={() => setSaved(true)} reload={() => { setSaved(false); void profile.refetch(); }} />
      <SectionHeader title={t(key('security'))} /><PasswordForm />
      <SectionHeader title={t(key('region'))} /><AppCard><SettingRow title={t('settings:languageTitle')} icon={Globe} onPress={() => router.push('/language')} /><SettingRow title={t(key('timezoneLink'))} subtitle={settings.data?.timeZoneId} icon={Clock} onPress={() => router.push('/notifications')} /></AppCard>
      <SectionHeader title={t(key('notifications'))} /><AppCard><SettingRow title={t(key('notifications'))} icon={Bell} onPress={() => router.push('/notifications')} /></AppCard>
      <SectionHeader title={t(key('membership'))} />{membership.isError ? <FinanceError error={membership.error} retry={() => { void membership.refetch(); }} /> : !membership.data ? <LoadingSkeleton /> : <AppCard><AppText variant="section">{t(key(membership.data.isPremium ? 'premium' : 'free'))}</AppText>{membership.data.accessEndsAtUtc && <AppText>{t(key('expires'), { value0: new Intl.DateTimeFormat(currentLocale(), { dateStyle: 'medium' }).format(new Date(membership.data.accessEndsAtUtc)) })}</AppText>}<AppText style={{ marginVertical: 12 }}>{t(key('limits'), { value0: membership.data.limits.activePaymentPlans, value1: membership.data.limits.activeRemindersPerPlan, value2: membership.data.limits.activeTrackedSubscriptions })}</AppText><AppButton title={t(key('membership'))} variant="secondary" onPress={() => router.push('/premium')} /><AppButton title={t(key('manageMembership'))} variant="ghost" onPress={() => { setLinkError(false); void Linking.openURL(Platform.OS === 'ios' ? 'https://apps.apple.com/account/subscriptions' : 'https://play.google.com/store/account/subscriptions').catch(() => setLinkError(true)); }} />{linkError && <AppText tone="danger" accessibilityRole="alert">{t(key('requestFailed'))}</AppText>}</AppCard>}
      <SectionHeader title={t(key('actions'))} /><AppCard><AppText tone="warning">{t(key('deleteWarning'))}</AppText>{deleting ? <PasswordForm deleting /> : <AppButton title={t(key('deleteAccount'))} variant="ghost" onPress={() => setDeleting(true)} />}</AppCard>
    </>}
  </AppScreen>;
}

export function ProfileForm({ profile, reload, onSaved }: { profile: Profile; reload(): void; onSaved?(): void }) {
  useLocaleRevision(); const mutation = useAccountMutation(); const busy = useRef(false);
  const [name, setName] = useState(profile.displayName); const [error, setError] = useState(''); const [saved, setSaved] = useState(false); const [blocked, setBlocked] = useState(false);
  async function save() {
    if (busy.current || blocked) return;
    if (!name.trim() || name.length > 100 || /\p{Cc}/u.test(name)) { setError('invalidName'); return; }
    busy.current = true; setError(''); setSaved(false);
    try { await mutation.mutateAsync({ kind: 'profile', displayName: name.trim(), rowVersion: profile.rowVersion }); setSaved(true); onSaved?.(); }
    catch (e) { setError(e instanceof ApiError && (e.status === 409 || e.kind === 'network' || e.kind === 'timeout') ? 'uncertainWrite' : 'requestFailed'); setBlocked(true); }
    finally { busy.current = false; mutation.reset(); }
  }
  return <AppCard><AppInput label={t(key('displayName'))} value={name} onChangeText={setName} maxLength={100} editable={!mutation.isPending && !blocked} autoComplete="name" /><AppText variant="caption" tone="muted" style={{ marginVertical: 12 }}>{t(key('emailReadOnly'))}</AppText>{error && <AppText tone="danger" accessibilityRole="alert">{t(key(error))}</AppText>}{saved && <AppText tone="success" accessibilityRole="alert">{t(key('profileSaved'))}</AppText>}<AppButton title={t(key('save'))} loading={mutation.isPending} disabled={blocked} onPress={() => { void save(); }} />{blocked && <AppButton title={t(key('reload'))} variant="secondary" onPress={reload} />}</AppCard>;
}

export function PasswordForm({ deleting = false }: { deleting?: boolean }) {
  const dialogs = useAppDialog();
  useLocaleRevision(); const mutation = useAccountMutation(); const busy = useRef(false);
  const [current, setCurrent] = useState(''); const [next, setNext] = useState(''); const [repeat, setRepeat] = useState(''); const [error, setError] = useState('');
  async function submit() {
    if (busy.current) return;
    if (!current || current.length > 128 || deleting && current.length < 12 || !deleting && (next.length < 12 || next.length > 128 || next === current)) { setError('passwordInvalid'); return; }
    if (!deleting && next !== repeat) { setError('passwordMismatch'); return; }
    busy.current = true; setError('');
    try { await mutation.mutateAsync(deleting ? { kind: 'delete', currentPassword: current } : { kind: 'password', currentPassword: current, newPassword: next, confirmation: repeat }); }
    catch (e) {
      if (e instanceof ApiError && (e.kind === 'network' || e.kind === 'timeout' || e.kind === 'session')) await session.invalidate(t(key('uncertainWrite')));
      else setError(e instanceof ApiError && e.code === 'current_password_invalid' ? 'wrongPassword' : 'requestFailed');
    } finally { busy.current = false; setCurrent(''); setNext(''); setRepeat(''); mutation.reset(); }
  }
  function confirm() {
    if (!deleting) { void submit(); return; }
    dialogs.alert(t(key('deleteAccount')), t(key('deleteWarning')), [{ text: t('common:cancel'), style: 'cancel' }, { text: t(key('confirmDelete')), style: 'destructive', onPress: () => submit() }]);
  }
  return <AppCard><AppInput label={t(key('currentPassword'))} value={current} onChangeText={setCurrent} maxLength={128} secureTextEntry autoComplete="off" editable={!mutation.isPending} />{!deleting && <><AppInput label={t(key('newPassword'))} value={next} onChangeText={setNext} maxLength={128} secureTextEntry autoComplete="off" editable={!mutation.isPending} /><AppInput label={t(key('repeatPassword'))} value={repeat} onChangeText={setRepeat} maxLength={128} secureTextEntry autoComplete="off" editable={!mutation.isPending} /><AppText tone="muted" variant="caption" style={{ marginVertical: 12 }}>{t(key('passwordPolicy'))}</AppText></>}{error && <AppText tone="danger" accessibilityRole="alert">{t(key(error))}</AppText>}<AppButton title={t(key(deleting ? 'confirmDelete' : 'changePassword'))} loading={mutation.isPending} onPress={confirm} />{dialogs.dialog}</AppCard>;
}
