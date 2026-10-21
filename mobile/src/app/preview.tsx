import { View, Pressable } from 'react-native';
import { router } from 'expo-router';
import { Check, FlaskConical } from 'lucide-react-native';
import { AppScreen, AppHeader, AppText, AppCard, AppButton, styles } from '../components/ui';
import { useUiStore, type PreviewState } from '../store/ui';
import { useTheme } from '../theme/use-theme';
import { usePreview } from '../hooks/use-preview';
export default function Preview() {
  const { colors } = useTheme(); const { enabled } = usePreview(); const selected = useUiStore((s) => s.preview); const set = useUiStore((s) => s.setPreview);
  return <AppScreen><AppHeader title="Tasarım önizlemesi" back /><AppCard><FlaskConical color={colors.accentText} size={28} /><AppText variant="section" style={{ marginTop: 16 }}>Örnek veriler, gerçek olmayan işlemler.</AppText><AppText tone="muted" style={{ marginTop: 12 }}>Bu alan yalnızca geliştirme görünümüdür. Oturum açılmaz; ödeme, abonelik veya bildirim işlemi yapılmaz.</AppText></AppCard>{enabled && <View style={{ gap: 12, marginTop: 24 }}>{[{ value: 'demo', title: 'Örnek içerik', subtitle: 'Dolu ekranların görünümü' }, { value: 'empty', title: 'Boş durum', subtitle: 'Henüz veri olmayan hesap' }, { value: 'loading', title: 'Yükleniyor', subtitle: 'İçerik hazırlanırken' }, { value: 'error', title: 'Bağlantı hatası', subtitle: 'Güvenli hata ve yeniden deneme' }].map((item) => <Pressable key={item.value} accessibilityRole="radio" accessibilityState={{ checked: selected === item.value }} onPress={() => set(item.value as PreviewState)}><AppCard style={[styles.row, { gap: 10, borderWidth: 1, borderColor: selected === item.value ? colors.accentText : colors.border }]}><View style={{ flex: 1 }}><AppText variant="label">{item.title}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 4 }}>{item.subtitle}</AppText></View>{selected === item.value && <Check color={colors.accentText} size={20} />}</AppCard></Pressable>)}</View>}<View style={{ marginTop: 24 }}><AppButton title="Görünüme dön" onPress={() => router.canGoBack() ? router.back() : router.replace('/')} /></View></AppScreen>;
}
