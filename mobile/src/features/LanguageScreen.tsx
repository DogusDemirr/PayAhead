import { useState } from 'react';
import { Alert, Platform, I18nManager, Pressable, View } from 'react-native';
import { reloadAppAsync } from 'expo';
import { Check } from 'lucide-react-native';
import { AppScreen, AppHeader, AppCard, AppText, AppInput, AppButton } from '../components/ui';
import { useTheme } from '../theme/use-theme';
import { languages } from '../i18n/languages';
import { chooseLanguage, isRTL, t, useLanguageStore, useLocaleRevision } from '../i18n/runtime';

export default function LanguageScreen() {
  useLocaleRevision(); const { colors } = useTheme(); const language = useLanguageStore(s => s.language);
  const pending = useLanguageStore(s => s.syncPending); const failed = useLanguageStore(s => s.storageFailed); const [search, setSearch] = useState('');
  const normalize = (value: string) => value.toLowerCase().normalize('NFD').replace(/\p{M}/gu, '');
  const rows = languages.filter(l => normalize(`${l.name} ${l.code}`).includes(normalize(search)));
  const restart = () => Alert.alert(t('settings:rtlRestartTitle'), t('settings:rtlRestartBody'), [{ text: t('common:cancel'), style: 'cancel' }, { text: t('settings:restart'), onPress: () => { I18nManager.allowRTL(true); I18nManager.forceRTL(isRTL()); void reloadAppAsync('language-direction').catch(() => undefined); } }]);
  return <AppScreen><AppHeader title={t('settings:languageTitle')} back /><AppInput label={t('settings:languageSearch')} value={search} onChangeText={setSearch} />
    <AppText tone="muted" style={{ marginVertical: 12 }}>{t('settings:languageFlagNote')}</AppText>
    {failed && <AppCard><AppText tone="danger" accessibilityRole="alert">{t('settings:languageStorageFailed')}</AppText></AppCard>}
    {pending && <AppText variant="caption" tone="muted">{t('settings:languagePending')}</AppText>}
    {Platform.OS !== 'web' && I18nManager.isRTL !== isRTL() && <AppCard><AppText tone="warning">{t('settings:rtlRestartBody')}</AppText><AppButton title={t('settings:restart')} variant="secondary" onPress={restart} /></AppCard>}
    <View style={{ gap: 10, marginTop: 12 }}>{rows.map(item => <Pressable key={item.code} accessibilityRole="radio" accessibilityLabel={item.name} accessibilityState={{ checked: language === item.code }} onPress={() => { void chooseLanguage(item.code); }} style={{ borderRadius: 18, padding: 18, minHeight: 76, borderWidth: 1, borderColor: language === item.code ? colors.accentText : colors.border, backgroundColor: language === item.code ? colors.accentSoft : colors.surface, flexDirection: 'row', alignItems: 'center', gap: 14 }}><AppText style={{ fontSize: 25 }}>{item.flag}</AppText><View style={{ flex: 1 }}><AppText variant="label">{item.name}</AppText><AppText variant="caption" tone="muted">{item.code}</AppText></View>{language === item.code && <Check accessibilityLabel={t('common:selected')} color={colors.accentText} size={22} />}</Pressable>)}{rows.length === 0 && <AppText>{t('settings:languageNotFound')}</AppText>}</View>
  </AppScreen>;
}
