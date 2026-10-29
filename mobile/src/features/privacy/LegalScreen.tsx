import { useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, SectionHeader } from '../../components/ui';
import { t, useLocaleRevision } from '../../i18n/runtime';
import { legalDocuments, legalVersion } from './documents';
import { Pressable, View } from 'react-native';
import { Check, Square } from 'lucide-react-native';
import { useTheme } from '../../theme/use-theme';
import { useRegistrationLegal } from './registration-legal';
export default function LegalScreen() {
  useLocaleRevision(); const { id, registration, attempt } = useLocalSearchParams<{ id?: string; registration?: string; attempt?: string }>();
  const state = useRegistrationLegal(); const { colors } = useTheme();
  const selected = id && Object.hasOwn(legalDocuments, id) ? id as keyof typeof legalDocuments : 'notice'; const doc = legalDocuments[selected];
  const canConfirm = registration === 'true' && !!attempt && state.attempt === attempt;
  const checked = state.selections[selected] === legalVersion;
  const confirmationKey = { notice: 'settings:registrationLegal.noticeCheck', privacy: 'settings:registrationLegal.privacyCheck', terms: 'settings:registrationLegal.termsCheck' }[selected];
  return <AppScreen><AppHeader title={t(doc.title)} back /><AppCard><AppText tone="warning">{t('settings:legal.draft')}</AppText><AppText variant="caption" tone="muted">{legalVersion}</AppText></AppCard>
    <SectionHeader title={t('settings:legal.identity')} /><AppCard><AppText>{t('settings:legal.pending')}</AppText></AppCard>
    <SectionHeader title={t(doc.title)} /><AppCard><AppText>{t(doc.body)}</AppText></AppCard>
    {selected !== 'terms' && <><SectionHeader title={t('settings:legal.retentionTitle')} /><AppCard><AppText>{t('settings:legal.retention')}</AppText></AppCard><SectionHeader title={t('settings:legal.transfersTitle')} /><AppCard><AppText>{t('settings:legal.transfers')}</AppText></AppCard></>}
    <SectionHeader title={t('settings:security.rights')} /><AppCard><AppText>{t('settings:legal.rights')}</AppText></AppCard>
    {canConfirm && <AppCard><Pressable accessibilityRole="checkbox" accessibilityState={{ checked }} accessibilityLabel={t(confirmationKey)} onPress={() => state.toggle(attempt!, selected)} style={{ minHeight: 56, paddingVertical: 8 }}><View style={{ flexDirection: 'row', gap: 12, alignItems: 'center' }}>{checked ? <Check size={26} color={colors.accentText} /> : <Square size={26} color={colors.muted} />}<AppText style={{ flex: 1 }}>{t(confirmationKey)}</AppText></View></Pressable></AppCard>}
  </AppScreen>;
}
