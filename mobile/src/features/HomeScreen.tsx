import { View } from 'react-native';
import { router } from 'expo-router';
import { Bell, Plus } from 'lucide-react-native';
import { AppScreen, AppText, AppButton, Brand, IconButton, AppCard, SectionHeader, LoadingSkeleton, EmptyState, MotionCard, styles } from '../components/ui';
import { useDashboard } from './payments/queries';
import { FinancialAmount, FinanceError, MonthlySummary, ProjectedRow } from './payments/components';
import { dateLabel, greeting } from '../utils/format';

export default function HomeScreen() {
  const { clock, summary, upcoming, overdue, hasOverdueRange, refresh } = useDashboard();
  const add = () => router.push('/add-payment');
  const content = () => {
    if (clock.isError || summary.isError) return <FinanceError error={clock.error ?? summary.error} retry={() => { void refresh(); }} />;
    if (!clock.data || !summary.data) return <LoadingSkeleton />;
    const data = summary.data;
    if (data.activePaymentPlanCount === 0 && data.paidPaymentCount === 0 && data.pendingPaymentCount === 0 && data.skippedPaymentCount === 0) return <EmptyState title="Daha sakin bir başlangıç" message="Bu ay aktif planın veya ödeme hareketin yok. Yeni bir planla daha rahat bir başlangıç yap." action="Ödeme ekle" onPress={add} />;
    return <>
      <AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{dateLabel(data.context.fromDate, true)} – {dateLabel(data.context.toDate, true)} · {data.context.timeZoneId}</AppText>
      {data.context.usedTimeZoneFallback && <AppText variant="caption" tone="warning" style={{ marginBottom: 12 }}>Hesap saat dilimi bulunamadı; tarihler UTC ile hesaplandı.</AppText>}
      <MotionCard><MonthlySummary summary={data} /></MotionCard><View style={{ marginTop: 12 }}><AppButton title="Tüm analizleri gör" variant="secondary" onPress={() => router.push('/analytics')} /></View>
      <View style={{ marginTop: 18 }}><AppButton title="Ödeme ekle" icon={Plus} onPress={add} /></View>
      <SectionHeader title="Bu ay ödendi" />
      <AppCard>{data.paidTotalsByCurrency.length ? data.paidTotalsByCurrency.map((total) => <View key={total.currencyCode} style={[styles.row, { flexWrap: 'wrap', gap: 8, marginVertical: 6 }]}><AppText tone="muted">{total.currencyCode}</AppText><FinancialAmount amount={total.amount} currency={total.currencyCode} /></View>) : <AppText tone="muted">Bu ay henüz ödenmiş tutar yok.</AppText>}<AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{data.paidPaymentCount} ödenen · {data.pendingPaymentCount} bekleyen · {data.skippedPaymentCount} atlanan</AppText></AppCard>
      <SectionHeader title="En yakın ödeme" />
      <AppCard>{clock.data.nextUpcomingPayment ? <ProjectedRow item={clock.data.nextUpcomingPayment} /> : <AppText tone="muted">Gösterilen yaklaşan tarih aralığında bekleyen ödeme yok.</AppText>}</AppCard>
      <SectionHeader title="Bu aydan gecikenler" />
      {!hasOverdueRange ? <AppCard><AppText tone="muted">Bu ay için geciken tarih aralığı yok.</AppText></AppCard> : overdue.isError ? <FinanceError error={overdue.error} retry={() => { void overdue.refetch(); }} /> : !overdue.data ? <LoadingSkeleton /> : <AppCard><AppText variant="caption" tone="warning">{overdue.data.totalCount} geciken ödeme · Bu ayın başından düne kadar</AppText>{overdue.data.items.map((item) => <ProjectedRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} overdue />)}{!overdue.data.totalCount && <AppText tone="muted" style={{ marginTop: 12 }}>Bu ay geciken ödemen yok.</AppText>}{overdue.data.totalCount > 3 && <AppText variant="caption" tone="muted">İlk 3 kayıt gösteriliyor.</AppText>}</AppCard>}
      <SectionHeader title="Yaklaşan ödemeler" action="Planlar" onPress={() => router.push('/(tabs)/payments')} />
      {upcoming.isError ? <FinanceError error={upcoming.error} retry={() => { void upcoming.refetch(); }} /> : !upcoming.data ? <LoadingSkeleton /> : <AppCard><AppText variant="caption" tone="muted">{dateLabel(upcoming.data.context.fromDate)} – {dateLabel(upcoming.data.context.toDate)} · {upcoming.data.totalCount} kayıt</AppText>{upcoming.data.items.map((item) => <ProjectedRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} />)}{!upcoming.data.totalCount && <AppText tone="muted" style={{ marginTop: 12 }}>Bu aralıkta yaklaşan ödemen yok.</AppText>}{upcoming.data.totalCount > 3 && <AppText variant="caption" tone="muted">İlk 3 kayıt gösteriliyor.</AppText>}</AppCard>}
    </>;
  };
  return <AppScreen refreshing={clock.isRefetching || summary.isRefetching || upcoming.isRefetching || overdue.isRefetching} onRefresh={() => { void refresh(); }}>
    <View style={[styles.row, { marginBottom: 28 }]}><Brand /><IconButton icon={Bell} label="Bildirimler" onPress={() => router.push('/notifications')} /></View>
    <AppText tone="muted">{greeting(new Date().getHours())}</AppText><AppText variant="title" accessibilityRole="header" style={{ marginTop: 4, marginBottom: 24 }}>Yarına bir adım önde.</AppText>{content()}
  </AppScreen>;
}
