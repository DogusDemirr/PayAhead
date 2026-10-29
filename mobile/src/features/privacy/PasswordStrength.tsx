import { View } from 'react-native';
import { AppText } from '../../components/ui';
import { useTheme } from '../../theme/use-theme';
import { t, useLocaleRevision } from '../../i18n/runtime';
export function PasswordStrength({ password }: { password: string }) {
  useLocaleRevision(); const { colors } = useTheme(); const score = password.length < 12 || new Set(password).size < 5 ? 1 : password.length < 20 ? 2 : 3;
  const label = score === 1 ? t('settings:security.weak') : score === 2 ? t('settings:security.fair') : t('settings:security.strong');
  return <View style={{ gap: 8 }}><View accessible accessibilityRole="progressbar" accessibilityLabel={t('settings:security.strength')} accessibilityValue={{ min: 0, max: 3, now: password ? score : 0, text: label }} style={{ height: 6, borderRadius: 3, backgroundColor: colors.border }}><View style={{ height: 6, borderRadius: 3, width: password ? `${score / 3 * 100}%` : '0%', backgroundColor: score === 1 ? colors.danger : score === 2 ? colors.warning : colors.success }} /></View><AppText variant="caption" tone="muted">{t('settings:security.strengthHelp')}</AppText></View>;
}
