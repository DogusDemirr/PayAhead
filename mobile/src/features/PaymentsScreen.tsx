import { useState } from 'react';
import { View, TextInput, Pressable } from 'react-native';
import { Search, Plus, SlidersHorizontal } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppText, AppCard, EmptyState, IconButton, styles } from '../components/ui';
import { PaymentListItem } from '../components/finance';
import { PreviewContent } from '../components/preview';
import { useTheme } from '../theme/use-theme';
import { previewPayments } from './preview-data';
import type { OccurrenceStatus } from '../types/api';

export default function PaymentsScreen() {
  const { colors } = useTheme(); const [search, setSearch] = useState(''); const [status, setStatus] = useState<OccurrenceStatus>('Pending'); const [currency, setCurrency] = useState('Tümü'); const [filters, setFilters] = useState(false);
  const items = previewPayments.filter((p) => p.status === status && p.name.toLocaleLowerCase('tr').includes(search.toLocaleLowerCase('tr')) && (currency === 'Tümü' || p.currencyCode === currency));
  return <AppScreen><AppHeader title="Ödemeler" subtitle="Her ödeme, yerli yerinde." right={<IconButton icon={Plus} label="Ödeme ekle" onPress={() => router.push('/add-payment')} />} />
    <View style={[styles.row, { gap: 12, marginBottom: 20 }]}><View style={[styles.row, { flex: 1, backgroundColor: colors.surface, minHeight: 52, borderRadius: 14, paddingHorizontal: 14, gap: 10 }]}><Search color={colors.muted} size={20} /><TextInput accessibilityLabel="Ödeme ara" placeholder="Ödeme veya abonelik ara" placeholderTextColor={colors.muted} value={search} onChangeText={setSearch} style={{ flex: 1, minHeight: 52, color: colors.text, fontFamily: 'Inter_400Regular', fontSize: 14 }} /></View><IconButton icon={SlidersHorizontal} label="Para birimi filtreleri" onPress={() => setFilters(!filters)} /></View>
    {filters && <View style={[styles.row, { justifyContent: 'flex-start', gap: 8, marginBottom: 16 }]}>{['Tümü', 'TRY', 'USD'].map((value) => <Pressable key={value} accessibilityRole="button" accessibilityState={{ selected: currency === value }} onPress={() => setCurrency(value)} style={{ minWidth: 60, minHeight: 48, borderRadius: 14, padding: 12, backgroundColor: value === currency ? colors.accentSoft : colors.surface }}><AppText variant="caption" tone={currency === value ? 'accentText' : 'muted'}>{value}</AppText></Pressable>)}</View>}
    <View style={[styles.row, { borderRadius: 14, backgroundColor: colors.elevated, padding: 4, gap: 4, marginBottom: 24 }]}>{[{ value: 'Pending', title: 'Yaklaşan' }, { value: 'Paid', title: 'Ödenen' }, { value: 'Skipped', title: 'Atlanan' }].map((tab) => <Pressable key={tab.value} accessibilityRole="tab" accessibilityState={{ selected: status === tab.value }} onPress={() => setStatus(tab.value as OccurrenceStatus)} style={{ flex: 1, minHeight: 48, alignItems: 'center', justifyContent: 'center', borderRadius: 11, backgroundColor: status === tab.value ? colors.surface : 'transparent' }}><AppText variant="caption" tone={status === tab.value ? 'text' : 'muted'}>{tab.title}</AppText></Pressable>)}</View>
    <PreviewContent emptyTitle="Henüz bir ödemen yok"><View style={[styles.row, { marginBottom: 12 }]}><AppText variant="section">{status === 'Pending' ? 'Yaklaşan ödemeler' : status === 'Paid' ? 'Ödeme geçmişi' : 'Atlanan ödemeler'}</AppText><AppText variant="caption" tone="muted">{items.length} kayıt</AppText></View>{items.length ? <AppCard style={{ paddingVertical: 0, paddingHorizontal: 16 }}>{items.map((item) => <PaymentListItem key={item.id} item={item} showStatus onPress={() => router.push({ pathname: '/payment', params: { id: item.id } })} />)}</AppCard> : <EmptyState title="Aradığını bulamadık" message="Farklı bir kelime veya para birimiyle tekrar ara." />}</PreviewContent>
  </AppScreen>;
}
