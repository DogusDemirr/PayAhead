import { useState } from 'react';
import { View, Pressable } from 'react-native';
import { ChevronLeft, ChevronRight } from 'lucide-react-native';
import { AppScreen, AppHeader, AppCard, AppText, CurrencyAmount, EmptyState, IconButton, SectionHeader, styles } from '../components/ui';
import { PaymentListItem } from '../components/finance';
import { PreviewContent } from '../components/preview';
import { previewPayments, relativeDay } from './preview-data';
import { useTheme } from '../theme/use-theme';
import { usePreview } from '../hooks/use-preview';
import { currencyTotals, dateKey, dateLabel } from '../utils/format';

export default function CalendarScreen() {
  const { colors } = useTheme(); const { state } = usePreview(); const initialDay = relativeDay(1).split('-').map(Number); const [month, setMonth] = useState(new Date(initialDay[0], initialDay[1] - 1, 1)); const [selected, setSelected] = useState(relativeDay(1));
  const count = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate(); const offset = (month.getDay() + 6) % 7;
  const payments = state === 'demo' ? previewPayments : []; const dayItems = payments.filter((item) => item.date === selected);
  function changeMonth(delta: number) { const next = new Date(month.getFullYear(), month.getMonth() + delta, 1); setMonth(next); setSelected(dateKey(next.getFullYear(), next.getMonth(), 1)); }
  return <AppScreen><AppHeader title="Takvim" subtitle="Planlarını bir bakışta gör." /><PreviewContent emptyTitle="Takvimin yeni planlara hazır" emptyMessage="Henüz bir ödeme planın yok. Eklediğinde takvimde görünecek.">
    <AppCard style={{ paddingHorizontal: 12 }}><View style={[styles.row, { paddingHorizontal: 4, marginBottom: 18 }]}><AppText variant="section" style={{ textTransform: 'capitalize' }}>{new Intl.DateTimeFormat('tr-TR', { month: 'long', year: 'numeric' }).format(month)}</AppText><View style={{ flexDirection: 'row' }}><IconButton icon={ChevronLeft} label="Önceki ay" onPress={() => changeMonth(-1)} /><IconButton icon={ChevronRight} label="Sonraki ay" onPress={() => changeMonth(1)} /></View></View>
      <View style={{ flexDirection: 'row', marginBottom: 8 }}>{['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'].map((label) => <AppText key={label} variant="caption" tone="muted" style={{ width: '14.2857%', textAlign: 'center' }}>{label}</AppText>)}</View>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap' }}>{Array.from({ length: offset + count }, (_, index) => { const day = index - offset + 1; if (day < 1) return <View key={`blank-${index}`} style={{ width: '14.2857%', height: 52 }} />; const key = dateKey(month.getFullYear(), month.getMonth(), day); const active = selected === key; const hasPayment = payments.some((item) => item.date === key); return <Pressable key={key} accessibilityRole="button" accessibilityLabel={`${day} ${new Intl.DateTimeFormat('tr-TR', { month: 'long' }).format(month)}${hasPayment ? ', ödeme var' : ''}`} accessibilityState={{ selected: active }} onPress={() => setSelected(key)} style={{ width: '14.2857%', height: 52, alignItems: 'center', justifyContent: 'center' }}><View style={{ width: 36, height: 36, alignItems: 'center', justifyContent: 'center', borderRadius: 13, backgroundColor: active ? colors.navy : 'transparent' }}><AppText variant="label" tone={active ? 'onNavy' : 'text'}>{day}</AppText></View><View style={{ height: 4, width: 4, borderRadius: 2, backgroundColor: hasPayment ? colors.accentText : 'transparent', marginTop: 2 }} /></Pressable>; })}</View>
      <View style={[styles.row, { justifyContent: 'center', gap: 6, marginTop: 16 }]}><View style={{ width: 5, height: 5, borderRadius: 3, backgroundColor: colors.accentText }} /><AppText variant="caption" tone="muted">Ödeme planlanan gün</AppText></View></AppCard>
    <SectionHeader title={`${dateLabel(selected, true)} ödemeleri`} />
    {dayItems.length ? <><AppCard style={{ paddingVertical: 0 }}>{dayItems.map((item) => <PaymentListItem key={item.id} item={item} />)}</AppCard><AppCard style={{ marginTop: 12 }}>{currencyTotals(dayItems).map((total) => <View key={total.currencyCode} style={[styles.row, { marginVertical: 4 }]}><AppText tone="muted">Günlük plan tutarı · {total.currencyCode}</AppText><CurrencyAmount amount={total.amount} currency={total.currencyCode} /></View>)}</AppCard></> : <EmptyState title="Bugün için ödeme yok" message="Takvimde işaretli bir günü seçerek ödemelerini inceleyebilirsin." />}
  </PreviewContent></AppScreen>;
}
