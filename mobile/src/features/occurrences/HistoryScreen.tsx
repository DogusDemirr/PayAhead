import { useState } from 'react';
import { View } from 'react-native';
import { AppScreen, AppHeader, AppButton, AppCard, AppText, EmptyState, LoadingSkeleton } from '../../components/ui';
import { useFinanceClock, useHistory } from '../payments/queries';
import { FinanceError } from '../payments/components';
import { monthRange } from '../payments/dates';
import type { OccurrenceStatus } from '../payments/contracts';
import { MonthNavigation } from './MonthNavigation';
import { OccurrenceRow } from './shared';
export default function HistoryScreen() {
  const clock = useFinanceClock();
  if (clock.isError) return <AppScreen><AppHeader title="Ödeme geçmişi" back /><FinanceError error={clock.error} retry={() => { void clock.refetch(); }} /></AppScreen>;
  if (!clock.data) return <AppScreen><LoadingSkeleton /></AppScreen>;
  return <HistoryBody today={clock.data.context.today} />;
}
export function HistoryBody({ today }: { today: string }) {
  const [month, setMonth] = useState(today.slice(0, 7) + '-01'); const [status, setStatus] = useState<OccurrenceStatus>('Paid'); const [page, setPage] = useState(1);
  const range = monthRange(month); const end = range.toDate > today ? today : range.toDate;
  const query = useHistory(range.fromDate, end, status, page);
  return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title="Ödeme geçmişi" subtitle="Planlanan tarihe göre dönem görünümü." back />
    <MonthNavigation month={month} maximum={today.slice(0, 7) + '-01'} onChange={value => { setMonth(value); setPage(1); }} />
    <View style={{ gap: 8, marginBottom: 16 }}>{(['Paid', 'Skipped', 'Pending'] as const).map(value => <AppButton key={value} title={({ Paid: 'Ödendi', Skipped: 'Atlandı', Pending: 'Bekliyor / gecikti' })[value] + (status === value ? ' ✓' : '')} variant={status === value ? 'primary' : 'secondary'} onPress={() => { setStatus(value); setPage(1); }} />)}</View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 16 }}>Gelecek plan tarihleri gösterilmez. Bekleyenler, kaydedilmemiş tekrarları da içerir. Sıra: planlanan tarih, ardından ödeme adı.</AppText>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }} /> : !query.data ? <LoadingSkeleton /> : <><AppText variant="caption" tone="muted">{query.data.totalCount} hareket · Sayfa {page}/{Math.max(1, query.data.totalPages)}</AppText>{query.data.items.length ? <AppCard>{query.data.items.map(item => <OccurrenceRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} today={today} />)}</AppCard> : <EmptyState title="Bu dönemde hareket yok" message="Başka bir durum veya ay seçebilirsin." />}<View style={{ gap: 8, marginTop: 16 }}><AppButton title="Önceki sayfa" variant="secondary" disabled={page <= 1 || query.isFetching} onPress={() => setPage(p => p - 1)} /><AppButton title="Sonraki sayfa" variant="secondary" disabled={page >= query.data.totalPages || query.isFetching} onPress={() => setPage(p => p + 1)} /></View></>}
  </AppScreen>;
}
