import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { Plus } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppInput, AppButton, IconButton, SectionHeader, EmptyState, LoadingSkeleton } from '../components/ui';
import { FinanceError } from './payments/components';
import { useEntitlements } from './payments/queries';
import { currencyCodes } from './payments/currencies';
import { useSubscriptions, useSubscriptionSummary } from './subscriptions/queries';
import { SubscriptionRow, SubscriptionTotals, statusLabels } from './subscriptions/components';
import type { SubscriptionStatus } from './subscriptions/contracts';
import { dateLabel } from '../utils/format';
export default function SubscriptionsScreen() {
  const [search, setSearch] = useState(''); const [debounced, setDebounced] = useState(''); const [page, setPage] = useState(1); const [status, setStatus] = useState<SubscriptionStatus>();
  const [currency, setCurrency] = useState(''); const [appliedCurrency, setAppliedCurrency] = useState<string>(); const [filterError, setFilterError] = useState('');
  useEffect(() => { const timer = setTimeout(() => { setDebounced(search.trim()); setPage(1); }, 300); return () => clearTimeout(timer); }, [search]);
  const list = useSubscriptions({ pageNumber: page, pageSize: 20, status, search: debounced || undefined, currencyCode: appliedCurrency }); const summary = useSubscriptionSummary(); const entitlement = useEntitlements();
  const refresh = () => { void list.refetch(); void summary.refetch(); void entitlement.refetch(); };
  return <AppScreen refreshing={list.isRefetching || summary.isRefetching} onRefresh={refresh}><AppHeader title="Abonelikler" subtitle="Küçük tutarlar, büyük bir netlik." right={<IconButton icon={Plus} label="Abonelik ekle" onPress={() => router.push('/add-subscription')} />} />
    {summary.isError ? <FinanceError error={summary.error} retry={() => { void summary.refetch(); }} /> : !summary.data ? <LoadingSkeleton /> : <><SubscriptionTotals data={summary.data} /><AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>Hesap tarihi: {dateLabel(summary.data.context.today, false, true)} · {summary.data.context.timeZoneId}{summary.data.context.usedTimeZoneFallback ? ' · UTC yedeği' : ''}</AppText>
      {entitlement.data && <AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>{entitlement.data.plan} · {summary.data.activeSubscriptionCount + summary.data.trialSubscriptionCount}/{entitlement.data.limits.activeTrackedSubscriptions} aktif/deneme limiti</AppText>}
      <View style={{ marginTop: 14, gap: 8 }}><AppButton title={`${summary.data.expiringTrialsCount} yaklaşan deneme bitişi`} variant="secondary" onPress={() => router.push('/subscription-trials')} /><AppButton title="Abonelik ekle" onPress={() => router.push('/add-subscription')} /></View>
      {summary.data.nextRenewal && <AppCard style={{ marginTop: 16 }}><AppText tone="muted" variant="caption">EN YAKIN ÜCRETLİ YENİLEME</AppText><SubscriptionRow record={summary.data.nextRenewal} /></AppCard>}</>}
    <SectionHeader title="Takip kayıtların" /><AppInput label="Abonelik veya sağlayıcı ara" value={search} onChangeText={setSearch} maxLength={150} />
    <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginVertical: 14 }}>{([undefined, 'Active', 'Trial', 'Cancelled', 'Expired'] as const).map(value => <View key={value ?? 'all'} style={{ minWidth: '45%', flexGrow: 1 }}><AppButton title={(value ? statusLabels[value] : 'Tümü') + (status === value ? ' ✓' : '')} variant={status === value ? 'primary' : 'secondary'} onPress={() => { setStatus(value); setPage(1); }} /></View>)}</View>
    <View style={{ gap: 8, marginBottom: 16 }}><AppInput label="Abonelik para birimi filtresi" value={currency} onChangeText={setCurrency} maxLength={3} autoCapitalize="characters" placeholder="Tümü için boş bırak" error={filterError || undefined} /><AppButton title="Abonelik filtresini uygula" variant="secondary" onPress={() => { const code = currency.trim().toUpperCase(); if (code && !currencyCodes.includes(code)) { setFilterError('Geçerli ISO 4217 kodu gir.'); return; } setFilterError(''); setAppliedCurrency(code || undefined); setPage(1); }} /></View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 10 }}>Sunucu sırası: en yeni oluşturulan önce. Özet tüm takip kayıtlarını kapsar; liste filtrelerinden bağımsızdır.</AppText>
    {list.isError ? <FinanceError error={list.error} retry={() => { void list.refetch(); }} /> : !list.data ? <LoadingSkeleton /> : <><AppText variant="caption" tone="muted">{list.data.totalCount} abonelik · Sayfa {page}/{Math.max(1, list.data.totalPages)}</AppText>{list.data.items.length ? <AppCard>{list.data.items.map(record => <SubscriptionRow key={record.id} record={record} />)}</AppCard> : <EmptyState title="Burada henüz abonelik yok" message="Filtrelerini değiştirebilir veya takip etmek istediğin bir abonelik ekleyebilirsin." action="Yeni abonelik" onPress={() => router.push('/add-subscription')} />}<View style={{ gap: 8, marginTop: 16 }}><AppButton title="Önceki abonelik sayfası" variant="secondary" disabled={page <= 1 || list.isFetching} onPress={() => setPage(p => p - 1)} /><AppButton title="Sonraki abonelik sayfası" variant="secondary" disabled={page >= list.data.totalPages || list.isFetching} onPress={() => setPage(p => p + 1)} /></View></>}
  </AppScreen>;
}
