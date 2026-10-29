import { useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, SectionHeader } from '../../components/ui';
import { t, useLocaleRevision } from '../../i18n/runtime';
import { legalDocuments, legalVersion } from './documents';
export default function LegalScreen() {
  useLocaleRevision(); const { id } = useLocalSearchParams<{ id?: string }>();
  const selected = id && Object.hasOwn(legalDocuments, id) ? id as keyof typeof legalDocuments : 'notice'; const doc = legalDocuments[selected];
  return <AppScreen><AppHeader title={t(doc.title)} back /><AppCard><AppText tone="warning">{t('settings:legal.draft')}</AppText><AppText variant="caption" tone="muted">{legalVersion}</AppText></AppCard>
    <SectionHeader title={t('settings:legal.identity')} /><AppCard><AppText>{t('settings:legal.pending')}</AppText></AppCard>
    <SectionHeader title={t(doc.title)} /><AppCard><AppText>{t(doc.body)}</AppText></AppCard>
    {selected !== 'terms' && <><SectionHeader title={t('settings:legal.retentionTitle')} /><AppCard><AppText>{t('settings:legal.retention')}</AppText></AppCard><SectionHeader title={t('settings:legal.transfersTitle')} /><AppCard><AppText>{t('settings:legal.transfers')}</AppText></AppCard></>}
    <SectionHeader title={t('settings:security.rights')} /><AppCard><AppText>{t('settings:legal.rights')}</AppText></AppCard>
  </AppScreen>;
}
