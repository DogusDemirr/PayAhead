import { t, useLocaleRevision } from "../i18n/runtime";
import { View, Pressable } from 'react-native';
import { router } from 'expo-router';
import { Check, FlaskConical } from 'lucide-react-native';
import { AppScreen, AppHeader, AppText, AppCard, AppButton, styles } from '../components/ui';
import { useUiStore, type PreviewState } from '../store/ui';
import { useTheme } from '../theme/use-theme';
import { usePreview } from '../hooks/use-preview';
export default function Preview() {
    useLocaleRevision();
    const { colors } = useTheme();
    const { enabled } = usePreview();
    const selected = useUiStore((s) => s.preview);
    const set = useUiStore((s) => s.setPreview);
    return <AppScreen><AppHeader title={t("common:m_87c8b717052f")} back/><AppCard><FlaskConical color={colors.accentText} size={28}/><AppText variant="section" style={{ marginTop: 16 }}>{t("common:m_af1af17b6039")}</AppText><AppText tone="muted" style={{ marginTop: 12 }}>{t("common:m_f6acdcf3a45e")}</AppText></AppCard>{enabled && <View style={{ gap: 12, marginTop: 24 }}>{[{ value: 'demo', title: t("common:m_44a04da0609b"), subtitle: t("common:m_8413667ec6c2") }, { value: 'empty', title: t("common:m_2d74c208f0c3"), subtitle: t("common:m_3c9870336f1d") }, { value: 'loading', title: t("common:m_daa069d849f4"), subtitle: t("common:m_3745003f5a43") }, { value: 'error', title: t("common:m_870a46c3db7b"), subtitle: t("common:m_02490070e7be") }].map((item) => <Pressable key={item.value} accessibilityRole="radio" accessibilityState={{ checked: selected === item.value }} onPress={() => set(item.value as PreviewState)}><AppCard style={[styles.row, { gap: 10, borderWidth: 1, borderColor: selected === item.value ? colors.accentText : colors.border }]}><View style={{ flex: 1 }}><AppText variant="label">{item.title}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 4 }}>{item.subtitle}</AppText></View>{selected === item.value && <Check color={colors.accentText} size={20}/>}</AppCard></Pressable>)}</View>}<View style={{ marginTop: 24 }}><AppButton title={t("common:m_c1081025bfce")} onPress={() => router.canGoBack() ? router.back() : router.replace('/')}/></View></AppScreen>;
}
