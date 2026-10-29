import { Pressable, View } from 'react-native';
import { Check, ChevronRight, FileText } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppText, styles } from '../../components/ui';
import { useTheme } from '../../theme/use-theme';
import { isRTL, t, useLocaleRevision } from '../../i18n/runtime';
import { legalDocuments, legalVersion } from './documents';
import { documentIds, useRegistrationLegal } from './registration-legal';

export function RegistrationDocuments({ showErrors }: { showErrors: boolean }) {
  useLocaleRevision(); const { colors } = useTheme(); const { attempt, selections } = useRegistrationLegal();
  return <View style={{ gap: 10 }}><AppText variant="section">{t('settings:security.legal')}</AppText><AppText variant="caption" tone="warning">{t('settings:legal.registration')}</AppText><AppText variant="caption" tone="muted">{t('settings:registrationLegal.info')}</AppText>
    {showErrors && documentIds.some(id => selections[id] !== legalVersion) && <AppText tone="danger" accessibilityRole="alert">{t('settings:registrationLegal.required')}</AppText>}
    {documentIds.map(id => {
      const confirmed = selections[id] === legalVersion; const missing = showErrors && !confirmed;
      const status = confirmed ? t('settings:registrationLegal.confirmed') : missing ? t('settings:registrationLegal.missing') : t('settings:registrationLegal.review');
      return <Pressable key={id} accessibilityRole="button" accessibilityLabel={`${t(legalDocuments[id].title)}. ${status}`} disabled={!attempt} onPress={() => router.push({ pathname: '/legal', params: { id, registration: 'true', attempt: attempt! } })} style={{ minHeight: 76, padding: 16, borderRadius: 18, borderWidth: missing ? 2 : 1, borderColor: missing ? colors.danger : confirmed ? colors.accentText : colors.border, backgroundColor: colors.surface }}>
        <View style={[styles.row, { gap: 12 }]}>{confirmed ? <Check color={colors.accentText} size={24} /> : <FileText color={missing ? colors.danger : colors.muted} size={24} />}<View style={{ flex: 1, gap: 4 }}><AppText variant="label">{t(legalDocuments[id].title)}</AppText><AppText variant="caption" tone={missing ? 'danger' : confirmed ? 'accentText' : 'muted'}>{status}</AppText></View><ChevronRight size={20} color={colors.muted} style={{ transform: [{ scaleX: isRTL() ? -1 : 1 }] }} /></View>
      </Pressable>;
    })}</View>;
}
