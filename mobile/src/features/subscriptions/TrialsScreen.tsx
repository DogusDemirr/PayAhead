import { useState } from 'react';
import { View } from 'react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, LoadingSkeleton, EmptyState } from '../../components/ui';
import { FinancialAmount, FinanceError } from '../payments/components';
import { useExpiringTrials } from './queries';
import { dateLabel } from '../../utils/format';
export default function TrialsScreen() {
  const [days, setDays] = useState(7); const [page, setPage] = useState(1); const query = useExpiringTrials(days);
  const totalPages = Math.max(1, Math.ceil((query.data?.length ?? 0) / 20)); const currentPage = Math.min(page, totalPages);
  return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title="Denemen bitmeden" subtitle="İlk ücretli güne hazırlıklı ol." back /><View style={{ gap: 8, marginBottom: 20 }}>{[1, 3, 7].map(value => <AppButton key={value} title={`${value} gün içinde${days === value ? ' ✓' : ''}`} variant={days === value ? 'primary' : 'secondary'} onPress={() => { setDays(value); setPage(1); }} />)}</View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 16 }}>Sunucunun hesap saat dilimine göre başlamış ve halen denemede olan kayıtlar. Bitiş günü dahil; iptal ve sona erenler bu listede değildir.</AppText>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }} /> : !query.data ? <LoadingSkeleton /> : query.data.length ? <>{query.data.slice((currentPage - 1) * 20, currentPage * 20).map(item => <AppCard key={item.subscriptionId} style={{ marginBottom: 14 }}><AppText variant="section">{item.name}</AppText><AppText tone="muted">{item.providerName}</AppText><AppText tone="warning" style={{ marginVertical: 12 }}>{item.daysRemaining === 0 ? 'Denemenin son günü' : `${item.daysRemaining} gün kaldı`}</AppText><AppText>Deneme bitişi: {dateLabel(item.trialEndDate, true, true)}</AppText><AppText>İlk ücretli ödeme: {dateLabel(item.firstBillingDate, true, true)}</AppText><View style={{ marginVertical: 12 }}><FinancialAmount amount={item.amount} currency={item.currencyCode} /></View><AppButton title={`${item.name} detayını gör`} variant="secondary" onPress={() => router.push({ pathname: '/subscription', params: { id: item.subscriptionId } })} /></AppCard>)}{totalPages > 1 && <View style={{ gap: 8 }}><AppText>Sayfa {currentPage}/{totalPages}</AppText><AppButton title="Önceki deneme sayfası" variant="secondary" disabled={currentPage <= 1} onPress={() => setPage(currentPage - 1)} /><AppButton title="Sonraki deneme sayfası" variant="secondary" disabled={currentPage >= totalPages} onPress={() => setPage(currentPage + 1)} /></View>}</> : <EmptyState title="Bu aralıkta bitecek deneme yok" message="Başka bir aralık seçebilir veya abonelik listeni inceleyebilirsin." />}
  </AppScreen>;
}
