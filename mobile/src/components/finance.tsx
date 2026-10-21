import { Pressable, View } from 'react-native';
import { ArrowUpRight, Cloud, GraduationCap, House, Music2, Wifi, Zap } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { AppCard, AppText, CurrencyAmount, StatusBadge, styles } from './ui';
import { useTheme } from '../theme/use-theme';
import { dateLabel } from '../utils/format';
import type { Category, PaymentPreview, SubscriptionPreview } from '../features/preview-data';

const icons = { home: House, internet: Wifi, music: Music2, cloud: Cloud, energy: Zap, education: GraduationCap };
export function CategoryIcon({ category }: { category: Category }) {
  const { colors } = useTheme(); const Icon = icons[category];
  const color = category === 'music' || category === 'education' ? colors.lavender : category === 'internet' || category === 'cloud' ? colors.blue : colors.accentText;
  const backgroundColor = category === 'music' || category === 'education' ? colors.lavenderSoft : category === 'internet' || category === 'cloud' ? colors.blueSoft : colors.accentSoft;
  return <View style={{ width: 46, height: 46, borderRadius: 15, backgroundColor, alignItems: 'center', justifyContent: 'center' }}><Icon color={color} size={22} strokeWidth={1.7} /></View>;
}
export function PaymentListItem({ item, onPress, showStatus = false }: { item: PaymentPreview; onPress?: () => void; showStatus?: boolean }) {
  const { colors } = useTheme();
  const content = <View style={[styles.row, { minHeight: 82, gap: 12, paddingVertical: 14 }]}><CategoryIcon category={item.category} /><View style={{ flex: 1, gap: 4 }}><AppText variant="label">{item.name}</AppText><AppText variant="caption" tone="muted">{dateLabel(item.date)} · {item.subtitle}</AppText>{showStatus && <StatusBadge label={item.status === 'Paid' ? 'Ödendi' : item.status === 'Skipped' ? 'Atlandı' : 'Yaklaşıyor'} tone={item.status === 'Paid' ? 'success' : item.status === 'Skipped' ? 'muted' : 'accent'} />}</View><View style={{ alignItems: 'flex-end', maxWidth: '38%' }}><CurrencyAmount amount={item.amount} currency={item.currencyCode} /><AppText variant="caption" tone="muted">{item.currencyCode}</AppText></View></View>;
  return onPress ? <Pressable accessibilityRole="button" accessibilityLabel={`${item.name}, ${dateLabel(item.date)}, ${item.amount} ${item.currencyCode}`} onPress={onPress} style={({ pressed }) => ({ opacity: pressed ? .6 : 1, borderBottomWidth: .5, borderBottomColor: colors.border })}>{content}</Pressable> : content;
}
export function SubscriptionCard({ item, onPress }: { item: SubscriptionPreview; onPress: () => void }) {
  const { colors } = useTheme();
  return <Pressable accessibilityRole="button" accessibilityLabel={`${item.name}, ${item.trialDays ? `${item.trialDays} gün deneme` : 'aktif abonelik'}`} onPress={onPress} style={({ pressed }) => ({ opacity: pressed ? .75 : 1, marginBottom: 12 })}><AppCard><View style={[styles.row, { gap: 12 }]}><CategoryIcon category={item.category} /><View style={{ flex: 1 }}><AppText variant="label">{item.name}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 4 }}>{item.provider}</AppText></View><ArrowUpRight color={colors.muted} size={18} /></View><View style={[styles.row, { marginTop: 20, alignItems: 'flex-end' }]}><View style={{ flex: 1, gap: 6 }}><StatusBadge label={item.trialDays ? `${item.trialDays} gün ücretsiz` : 'Aktif'} tone={item.trialDays ? 'warning' : 'accent'} /><AppText variant="caption" tone="muted">{dateLabel(item.renewal)} tarihinde yenilenir</AppText></View><View style={{ alignItems: 'flex-end' }}><CurrencyAmount amount={item.amount} currency={item.currencyCode} /><AppText variant="caption" tone="muted">/ ay</AppText></View></View></AppCard></Pressable>;
}
export function SummaryCard({ amount, currency, count, secondary }: { amount: number; currency: string; count: number; secondary?: string }) {
  const { colors } = useTheme();
  return <LinearGradient colors={[colors.navy, colors.decoration]} start={{ x: 0, y: 0 }} end={{ x: 1, y: 1 }} style={{ borderRadius: 24, padding: 24, overflow: 'hidden' }}>
    <View pointerEvents="none" style={{ position: 'absolute', width: 200, height: 200, borderRadius: 100, borderWidth: 1, borderColor: colors.onNavyMuted, opacity: .12, right: -80, top: -100 }} />
    <View pointerEvents="none" style={{ position: 'absolute', width: 160, height: 160, borderRadius: 80, borderWidth: 1, borderColor: colors.onNavyMuted, opacity: .12, right: -60, top: -80 }} />
    <View style={styles.row}><AppText variant="caption" tone="onNavyMuted" style={{ letterSpacing: 1.5 }}>ÖNÜMÜZDEKİ 30 GÜN</AppText><ArrowUpRight size={20} color={colors.accent} /></View>
    <AppText tone="onNavy" style={{ marginTop: 18, opacity: .85 }}>Yaklaşan ödemelerin</AppText>
    <View style={{ marginTop: 5 }}><CurrencyAmount amount={amount} currency={currency} large inverse /></View>
    <View style={[styles.row, { marginTop: 22, paddingTop: 16, borderTopWidth: .5, borderTopColor: colors.decoration, flexWrap: 'wrap', gap: 10 }]}><View style={[styles.row, { gap: 8 }]}><View style={{ height: 6, width: 6, borderRadius: 3, backgroundColor: colors.accent }} /><AppText variant="caption" tone="onNavyMuted">{count} ödeme · {currency}</AppText></View>{secondary && <AppText variant="caption" tone="onNavyMuted">Ayrıca {secondary}</AppText>}</View>
  </LinearGradient>;
}
