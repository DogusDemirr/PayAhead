import { useState } from 'react';
import { View } from 'react-native';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, AppInput, SectionHeader, EmptyState, LoadingSkeleton } from '../../components/ui';
import { useFinanceClock, useAnalytics } from '../payments/queries';
import { FinanceError, FinancialAmount } from '../payments/components';
import { MonthNavigation } from '../occurrences/MonthNavigation';
import { AmountChart, StatusChart } from './Charts';
import type { CurrencyAnalytics } from '../payments/contracts';
import { dateLabel } from '../../utils/format';
import { currencyCodes } from '../payments/currencies';
export default function AnalyticsScreen() {
  const clock = useFinanceClock();
  if (clock.isError) return <AppScreen><AppHeader title="Analizler" back /><FinanceError error={clock.error} retry={() => { void clock.refetch(); }} /></AppScreen>;
  if (!clock.data) return <AppScreen><LoadingSkeleton /></AppScreen>;
  return <AnalyticsBody today={clock.data.context.today} />;
}
export function AnalyticsBody({ today }: { today: string }) {
  const [month, setMonth] = useState(today.slice(0, 7) + '-01'); const [currency, setCurrency] = useState('TRY');
  const [filter, setFilter] = useState(''); const [applied, setApplied] = useState<string>(); const [filterError, setFilterError] = useState('');
  const [year, number] = month.split('-').map(Number); const query = useAnalytics(year, number, applied);
  const selected = query.data?.currencies.find(c => c.currencyCode === currency) ?? query.data?.currencies[0];
  return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title="Analizler" subtitle="Rakamların ardındaki düzen." back /><MonthNavigation month={month} onChange={value => { if (value >= '0001-06-01' && value <= '9999-12-01') setMonth(value); }} />
    <View style={{ gap: 8, marginBottom: 16 }}><AppInput label="Para birimi filtresi · isteğe bağlı" placeholder="Tümü için boş bırak · TRY / USD / EUR" value={filter} onChangeText={setFilter} maxLength={3} autoCapitalize="characters" error={filterError || undefined} /><AppButton title="Para birimi filtresini uygula" variant="secondary" onPress={() => { const code = filter.trim().toUpperCase(); if (code && !currencyCodes.includes(code)) { setFilterError('Geçerli ISO 4217 kodu gir.'); return; } setFilterError(''); setApplied(code || undefined); if (code) setCurrency(code); }} /></View>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }} /> : !query.data ? <LoadingSkeleton /> : !selected ? <EmptyState title="Analizlerin yeni planlara hazır" message="Seçilen altı ayda ödeme verisi yok. Başka bir dönem seçebilir veya yeni bir plan oluşturabilirsin." /> : <><View style={{ gap: 8, marginBottom: 16 }}>{query.data.currencies.map(c => <AppButton key={c.currencyCode} title={c.currencyCode + (c.currencyCode === selected.currencyCode ? ' ✓' : '')} variant={c.currencyCode === selected.currencyCode ? 'primary' : 'secondary'} onPress={() => setCurrency(c.currencyCode)} />)}</View><AppText variant="caption" tone="muted">{dateLabel(query.data.context.fromDate, false, true)} – {dateLabel(query.data.context.toDate, false, true)} · {query.data.context.timeZoneId}</AppText><AppText variant="caption" tone="muted" style={{ marginVertical: 12 }}>Tüm grafikler planlanan tarihin ayına göre gruplandırılır; ödeme zamanına göre nakit akışı değildir. Planlanan tutarlar güncel plan tanımına dayanır, geçmiş fiyat snapshot kaydı değildir. Geçmiş tutarlar güncel plan para birimi ve kategorisiyle gösterilir. Para birimleri ayrı tutulur. Geciken yalnızca seçili ay içindir.</AppText>{query.data.context.usedTimeZoneFallback && <AppText tone="warning">Tarihler UTC ile hesaplandı.</AppText>}<CurrencyAnalysis data={selected} /></>}
  </AppScreen>;
}
export function CurrencyAnalysis({ data }: { data: CurrencyAnalytics }) {
  const current = data.months[data.months.length - 1]; if (!current) return <EmptyState title="Dönem verisi bulunamadı" />;
  const totals = [{ label: 'Planlanan · seçili ay', amount: current.plannedAmount }, { label: 'Gerçek ödenen · seçili ay', amount: current.paidAmount }, { label: 'Bekleyen · geciken dahil', amount: current.pendingAmount }, { label: 'Bu aydan geciken', amount: current.overdueAmount }];
  return <><AppCard>{totals.map(row => <View key={row.label} style={{ gap: 6, marginVertical: 10 }}><AppText tone="muted">{row.label}</AppText><FinancialAmount amount={row.amount} currency={data.currencyCode} /></View>)}</AppCard>
    <SectionHeader title="6 aylık gerçek ödemeler" /><AppCard><AmountChart rows={data.months.map(m => ({ label: dateLabel(m.month, false, true), amount: m.paidAmount }))} currency={data.currencyCode} /><AppText variant="caption" tone="muted" style={{ marginTop: 16 }}>Yalnızca ödendi durumundaki gerçek tutarlar. Sıfır, bu kapsamdaki ödendi tutarının sıfır olduğu anlamına gelir.</AppText></AppCard>
    <SectionHeader title="Planlanan tutar trendi" /><AppCard><AmountChart rows={data.months.map(m => ({ label: dateLabel(m.month, false, true), amount: m.plannedAmount }))} currency={data.currencyCode} /></AppCard>
    <SectionHeader title="Bu ay ödeme durumları" /><AppCard><StatusChart paid={current.paidCount} pending={current.pendingCount} skipped={current.skippedCount} /></AppCard>
    <SectionHeader title="Bu ay ödenen kategoriler" />{data.paidCategories.length ? <AppCard><AmountChart rows={data.paidCategories.slice(0, 8)} currency={data.currencyCode} />{data.paidCategories.length > 8 && <AppText variant="caption" tone="muted">{data.paidCategories.length} kategoriden tutarı en yüksek 8 kategori gösteriliyor.</AppText>}</AppCard> : <AppCard><AppText tone="muted">Seçili ayda ödenmiş kategori verisi yok.</AppText></AppCard>}
    <SectionHeader title="En yüksek 5 plan" /><AppCard><AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>Seçili aydaki planlanan tekrar tutarları.</AppText>{data.topPlans.map((plan, i) => <View key={`${plan.label}-${i}`} style={{ marginVertical: 8, gap: 4 }}><AppText variant="label">{plan.label}</AppText><FinancialAmount amount={plan.amount} currency={data.currencyCode} /><AppText variant="caption" tone="muted">{plan.count} tekrar</AppText></View>)}</AppCard>
  </>;
}
