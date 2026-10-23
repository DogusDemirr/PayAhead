import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { Plus, SlidersHorizontal } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppInput, AppText, AppCard, AppButton, EmptyState, LoadingSkeleton, IconButton, styles } from '../components/ui';
import { useFinanceClock, usePaymentList } from './payments/queries';
import { FinanceError, PlanRow } from './payments/components';
import { nextPlanDate } from './payments/dates';
import { currencyCodes } from './payments/currencies';

export default function PaymentsScreen() {
  const [search, setSearch] = useState(''); const [applied, setApplied] = useState(''); const [page, setPage] = useState(1); const [active, setActive] = useState<boolean | undefined>(undefined);
  const [filtersOpen, setFiltersOpen] = useState(false); const [currency, setCurrency] = useState(''); const [category, setCategory] = useState('');
  const [extra, setExtra] = useState<{ currencyCode?: string; category?: string }>({}); const [filterError, setFilterError] = useState('');
  useEffect(() => { const timer = setTimeout(() => { setApplied(search.trim()); setPage(1); }, 300); return () => clearTimeout(timer); }, [search]);
  const query = usePaymentList({ pageNumber: page, pageSize: 20, search: applied || undefined, isActive: active, ...extra }); const clock = useFinanceClock();
  return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); void clock.refetch(); }}><AppHeader title="Ödemeler" subtitle="Her plan, yerli yerinde." right={<IconButton icon={Plus} label="Ödeme ekle" onPress={() => router.push('/add-payment')} />} />
    <AppInput label="Ödeme ara" placeholder="Ödeme adına göre ara" value={search} onChangeText={setSearch} maxLength={150} autoCorrect={false} />
    <View style={{ marginTop: 12 }}><AppButton title="Filtre seçenekleri" icon={SlidersHorizontal} variant="ghost" onPress={() => setFiltersOpen(!filtersOpen)} /></View>
    {filtersOpen && <AppCard style={{ gap: 14 }}><AppInput label="Para birimi filtresi" placeholder="Boş bırak veya TRY / USD / EUR" value={currency} onChangeText={setCurrency} maxLength={3} autoCapitalize="characters" /><AppInput label="Kategori filtresi" placeholder="Kategoriyle tam eşleşme" value={category} onChangeText={setCategory} maxLength={50} />{!!filterError && <AppText tone="danger" accessibilityRole="alert">{filterError}</AppText>}<AppButton title="Filtreleri uygula" variant="secondary" onPress={() => { const code = currency.trim().toUpperCase(); if (code && !currencyCodes.includes(code)) { setFilterError('Geçerli ISO 4217 kodu yaz.'); return; } setFilterError(''); setExtra({ currencyCode: code || undefined, category: category.trim() || undefined }); setPage(1); setFiltersOpen(false); }} /><AppButton title="Ek filtreleri temizle" variant="ghost" onPress={() => { setCurrency(''); setCategory(''); setExtra({}); setFilterError(''); setPage(1); }} /></AppCard>}
    <View style={[styles.row, { gap: 8, marginVertical: 20 }]}>{[{ label: 'Tümü', value: undefined }, { label: 'Aktif', value: true }, { label: 'Pasif', value: false }].map((option) => <FilterChip key={option.label} label={option.label} selected={active === option.value} onPress={() => { setActive(option.value); setPage(1); }} />)}</View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>Yeni eklenenler önce · Arama ve filtre sunucuda uygulanır.</AppText>
    {!!(extra.currencyCode || extra.category) && <AppText variant="caption" tone="accentText" style={{ marginBottom: 12 }}>{extra.currencyCode ?? 'Tüm para birimleri'} · {extra.category ?? 'Tüm kategoriler'}</AppText>}
    {search.trim() !== applied || query.isPending ? <LoadingSkeleton /> : query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }} /> : query.data?.items.length ? <>
      <AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{query.data.totalCount} plan · Sayfa {query.data.pageNumber}/{query.data.totalPages}</AppText>
      <AppCard>{query.data.items.map((plan) => <PlanRow key={plan.id} plan={plan} next={clock.data ? nextPlanDate(plan, clock.data.context.today) : undefined} />)}</AppCard>
      {clock.isError && <AppText tone="warning" variant="caption" style={{ marginTop: 12 }}>Hesap tarihi alınamadı; plan tarihleri gösterilemiyor.</AppText>}
      <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>Planlanan tarih, tekrar takviminden hesaplanır; ödenen/atlanan durumunu içermez.</AppText>
      <View style={[styles.row, { gap: 12, marginTop: 24 }]}><View style={{ flex: 1 }}><AppButton title="Önceki" variant="secondary" disabled={page <= 1} onPress={() => setPage(page - 1)} /></View><View style={{ flex: 1 }}><AppButton title="Sonraki" variant="secondary" disabled={page >= query.data.totalPages} onPress={() => setPage(page + 1)} /></View></View>
    </> : <EmptyState title={page > 1 ? 'Bu sayfada plan kalmadı' : applied || active !== undefined || extra.currencyCode || extra.category ? 'Bu filtrede plan yok' : 'İlk planına yer aç'} message="Ödemelerini eklediğinde tutarlarını ve tekrar takvimini burada görebileceksin." action={page > 1 ? 'Önceki sayfa' : 'Ödeme ekle'} onPress={() => page > 1 ? setPage(page - 1) : router.push('/add-payment')} />}
  </AppScreen>;
}
function FilterChip({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <View style={{ flex: 1 }}><AppButton title={label + (selected ? ' ✓' : '')} variant={selected ? 'primary' : 'secondary'} onPress={onPress} /></View>;
}
