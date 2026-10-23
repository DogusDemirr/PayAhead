import { useState } from 'react';
import { Pressable, View } from 'react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, EmptyState, LoadingSkeleton, SectionHeader } from '../components/ui';
import { useCalendar, useFinanceClock } from './payments/queries';
import { FinanceError } from './payments/components';
import { dayKey, daysInMonth } from './payments/dates';
import { localCalendarDate } from '../components/date-field';
import { MonthNavigation } from './occurrences/MonthNavigation';
import { OccurrenceRow, statusLabel } from './occurrences/shared';
import { useTheme } from '../theme/use-theme';
import { dateLabel } from '../utils/format';
export default function CalendarScreen() {
  const clock = useFinanceClock();
  if (clock.isError) return <AppScreen><AppHeader title="Takvim" /><FinanceError error={clock.error} retry={() => { void clock.refetch(); }} /></AppScreen>;
  if (!clock.data) return <AppScreen><LoadingSkeleton /></AppScreen>;
  return <CalendarBody today={clock.data.context.today} />;
}
export function CalendarBody({ today }: { today: string }) {
  const { colors } = useTheme(); const [month, setMonth] = useState(today.slice(0, 7) + '-01'); const [selected, setSelected] = useState(today);
  const [year, number] = month.split('-').map(Number); const query = useCalendar(year, number);
  const days = query.data?.days ?? []; const count = daysInMonth(year, number); const offset = (localCalendarDate(month).getDay() + 6) % 7;
  function selectMonth(value: string) { setMonth(value); setSelected(value); }
  return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title="Takvim" subtitle="Her ödemenin kendi hikâyesi." />
    <View style={{ gap: 8, marginBottom: 16 }}><AppButton title="Ödeme geçmişi" variant="secondary" onPress={() => router.push('/payment-history')} /><AppButton title="Bugüne dön" variant="ghost" onPress={() => { setMonth(today.slice(0, 7) + '-01'); setSelected(today); }} /></View>
    <AppCard style={{ paddingHorizontal: 8 }}><MonthNavigation month={month} onChange={selectMonth} />
      <View style={{ flexDirection: 'row' }}>{['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'].map(label => <AppText key={label} variant="caption" tone="muted" style={{ width: '14.2857%', textAlign: 'center' }}>{label}</AppText>)}</View>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap' }}>{Array.from({ length: offset + count }, (_, i) => {
        const day = i - offset + 1; if (day < 1) return <View key={i} style={{ width: '14.2857%', height: 58 }} />;
        const key = dayKey(year, number, day); const entries = days.find(d => d.date === key)?.payments ?? [];
        const labels = [...new Set(entries.map(p => statusLabel(p.status, p.dueDate, today)))];
        return <Pressable key={key} accessibilityRole="button" accessibilityLabel={`${key}${key === today ? ', bugün' : ''}${labels.length ? ', ' + labels.join(', ') : ''}`} accessibilityState={{ selected: selected === key }} onPress={() => setSelected(key)} style={{ width: '14.2857%', minHeight: 58, alignItems: 'center', justifyContent: 'center', gap: 4 }}><View style={{ borderRadius: 12, width: 36, height: 36, justifyContent: 'center', alignItems: 'center', backgroundColor: selected === key ? colors.navy : 'transparent', borderWidth: key === today ? 2 : 0, borderColor: colors.accentText }}><AppText tone={selected === key ? 'onNavy' : 'text'}>{day}</AppText></View><View style={{ flexDirection: 'row', gap: 2 }}>{labels.map(label => <View key={label} style={{ width: 5, height: 5, borderRadius: 3, backgroundColor: label === 'Ödendi' ? colors.success : label === 'Atlandı' ? colors.muted : label === 'Gecikti' ? colors.danger : colors.accentText }} />)}</View></Pressable>;
      })}</View><AppText variant="caption" tone="muted" style={{ padding: 8 }}>Yeşil: ödendi · Gri: atlandı · Kırmızı: gecikti · Teal: bekliyor. Durumlar gün seçildiğinde metinle de gösterilir.</AppText></AppCard>
    <SectionHeader title={dateLabel(selected, true, true)} />
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }} /> : !query.data ? <LoadingSkeleton /> : <>{query.data.context.usedTimeZoneFallback && <AppText tone="warning">Tarihler UTC ile hesaplandı.</AppText>}{days.find(d => d.date === selected)?.payments.length ? <AppCard>{days.find(d => d.date === selected)!.payments.map(item => <OccurrenceRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} today={query.data.context.today} />)}</AppCard> : <EmptyState title="Bu gün için ödeme yok" message="İşaretli bir günü seçerek ödeme durumunu inceleyebilirsin." />}</>}
  </AppScreen>;
}
