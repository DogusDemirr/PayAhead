import { View, Pressable } from 'react-native';
import { router } from 'expo-router';
import { ArrowRight, Bell, CalendarDays, Plus, Repeat2, Sparkles } from 'lucide-react-native';
import { AppScreen, AppText, Brand, IconButton, AppCard, SectionHeader, MotionCard, styles, Assurance } from '../components/ui';
import { SummaryCard, PaymentListItem } from '../components/finance';
import { PreviewContent } from '../components/preview';
import { useTheme } from '../theme/use-theme';
import { previewPayments, previewSubscriptions } from './preview-data';
import { currencyTotals, greeting, money } from '../utils/format';

export default function HomeScreen() {
  const { colors } = useTheme(); const pending = previewPayments.filter((item) => item.status === 'Pending');
  const totals = currencyTotals(pending); const primary = totals.find((total) => total.currencyCode === 'TRY')!;
  return <AppScreen>
    <View style={[styles.row, { marginBottom: 28 }]}><Brand /><IconButton icon={Bell} label="Bildirimler" onPress={() => router.push('/notifications')} /></View>
    <AppText tone="muted">{greeting(new Date().getHours())} ☀</AppText>
    <AppText variant="title" accessibilityRole="header" style={{ marginTop: 4, marginBottom: 24 }}>Yarına bir adım önde.</AppText>
    <PreviewContent emptyTitle="Daha sakin bir başlangıç" emptyMessage="Henüz ödeme planın yok. Ödemelerini eklediğinde yaklaşan tutarları burada görebileceksin.">
      <MotionCard><SummaryCard amount={primary.amount} currency="TRY" count={pending.filter((p) => p.currencyCode === 'TRY').length} secondary={money(5.99, 'USD')} /></MotionCard>
      <View style={[styles.row, { marginTop: 18, gap: 12 }]}>
        {[{ title: 'Ödeme ekle', icon: Plus, path: '/add-payment' as const }, { title: 'Takvimi aç', icon: CalendarDays, path: '/(tabs)/calendar' as const }].map(({ title, icon: Icon, path }) => <Pressable key={title} accessibilityRole="button" onPress={() => router.push(path)} style={({ pressed }) => ({ flex: 1, opacity: pressed ? .65 : 1, backgroundColor: colors.surface, borderRadius: 16, paddingVertical: 16, paddingHorizontal: 12, flexDirection: 'row', gap: 9, alignItems: 'center', justifyContent: 'center', minHeight: 56 })}><Icon size={19} color={colors.accentText} /><AppText variant="caption">{title}</AppText></Pressable>)}
      </View>
      <SectionHeader title="Sıradaki ödemeler" action="Tümünü gör" onPress={() => router.push('/(tabs)/payments')} />
      <AppCard style={{ paddingVertical: 0, paddingHorizontal: 16 }}>{pending.slice(0, 3).map((item) => <PaymentListItem key={item.id} item={item} onPress={() => router.push({ pathname: '/payment', params: { id: item.id } })} />)}</AppCard>
      <SectionHeader title="Aboneliklerine bir bakış" action="Detaylar" onPress={() => router.push('/(tabs)/subscriptions')} />
      <View style={[styles.row, { alignItems: 'stretch', gap: 12 }]}><AppCard style={{ flex: 1, padding: 16 }}><Repeat2 color={colors.accentText} size={21} /><AppText variant="title" style={{ marginTop: 14 }}>{previewSubscriptions.filter((s) => !s.trialDays).length}</AppText><AppText variant="caption" tone="muted">Aktif abonelik</AppText></AppCard><AppCard style={{ flex: 1, padding: 16 }}><Sparkles color={colors.lavender} size={21} /><AppText variant="title" style={{ marginTop: 14 }}>1</AppText><AppText variant="caption" tone="muted">Ücretsiz deneme</AppText></AppCard></View>
      <Pressable accessibilityRole="button" accessibilityLabel="Focus Pro denemesini incele" onPress={() => router.push('/(tabs)/subscriptions')} style={{ marginTop: 16 }}><AppCard style={{ backgroundColor: colors.warningSoft, flexDirection: 'row', gap: 12, alignItems: 'center', padding: 16 }}><View style={{ flex: 1 }}><AppText variant="label">Denemen bitmeden haberdar ol.</AppText><AppText variant="caption" tone="warning" style={{ marginTop: 5 }}>Focus Pro için 3 gün kaldı.</AppText></View><ArrowRight color={colors.warning} size={20} /></AppCard></Pressable>
      <Assurance text="Önceden gör. Rahatça planla." />
    </PreviewContent>
  </AppScreen>;
}
