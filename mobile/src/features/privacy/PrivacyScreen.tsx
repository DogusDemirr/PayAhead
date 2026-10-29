import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, SectionHeader } from '../../components/ui';
import { t, useLocaleRevision, currentLocale } from '../../i18n/runtime';
import { useSecuritySummary } from './queries';
import { FinanceError } from '../payments/components';

export function SecurityStatus() {
  useLocaleRevision(); const query = useSecuritySummary();
  if (query.isError) return <FinanceError error={query.error} retry={() => { void query.refetch(); }} />;
  return <AppCard><AppText>{t('settings:security.quotaInfo')}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>{t('settings:security.historyInfo')}</AppText>{query.data && <><AppText variant="label" style={{ marginTop: 12 }}>{t('settings:security.remaining', { value0: query.data.remainingChanges })}</AppText>{query.data.nextAvailableAtUtc && <AppText style={{ marginTop: 8 }}>{t('settings:security.next', { value0: new Intl.DateTimeFormat(currentLocale(), { dateStyle: 'medium', timeStyle: 'short', timeZone: 'UTC' }).format(new Date(query.data.nextAvailableAtUtc)) })}</AppText>}<AppText style={{ marginTop: 12 }}>{t('settings:security.sessions', { value0: query.data.activeSessions })}</AppText></>}<AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t('settings:security.sessionsInfo')}</AppText><AppButton title={t('settings:security.recovery')} variant="ghost" onPress={() => router.push('/forgot-password')} /></AppCard>;
}
export default function PrivacyScreen() {
  useLocaleRevision(); return <AppScreen><AppHeader title={t('settings:security.title')} back /><AppCard><AppText>{t('settings:security.accountInfo')}</AppText><AppText style={{ marginTop: 12 }}>{t('settings:security.avatarInfo')}</AppText></AppCard>
    <SectionHeader title={t('settings:security.password')} /><SecurityStatus /><AppButton title={t('settings:profile.changePassword')} onPress={() => router.push('/account')} />
    <SectionHeader title={t('settings:security.rights')} /><AppCard><AppText>{t('settings:security.rightsInfo')}</AppText><AppButton title={t('settings:profile.accountTitle')} variant="secondary" onPress={() => router.push('/account')} /></AppCard>
    <SectionHeader title={t('settings:profile.deleteAccount')} /><AppCard><AppText tone="warning">{t('settings:security.deleteInfo')}</AppText><AppButton title={t('settings:profile.accountTitle')} variant="ghost" onPress={() => router.push('/account')} /></AppCard>
  </AppScreen>;
}
