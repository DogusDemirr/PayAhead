import { View, Pressable } from 'react-native';
import { router } from 'expo-router';
import { AppCard, AppText, AppButton, StatusBadge, SectionHeader } from '../../components/ui';
import { FinanceError, FinancialAmount } from '../payments/components';
import { frequencyLabels } from '../payments/validation';
import { ApiError } from '../../services/api-error';
import { dateLabel } from '../../utils/format';
import { useSubscriptionSummary, useExpiringTrials } from './queries';
import type { Subscription, SubscriptionSummary } from './contracts';
export const statusLabels = { Trial: 'Ücretsiz deneme', Active: 'Aktif', Cancelled: 'İptal edildi', Expired: 'Sona erdi' } as const;
export function trialHint(record: Pick<Subscription, 'status' | 'trialStartDate' | 'trialEndDate' | 'trialDaysRemaining' | 'context'>) {
  if (!record.trialEndDate) return null;
  if (record.status === 'Cancelled') return 'Deneme takip kaydı iptal edildi.';
  if (record.status === 'Expired' || record.trialEndDate < record.context.today) return 'Deneme süresi bitti.';
  if (record.trialStartDate && record.trialStartDate > record.context.today) return `Deneme ${dateLabel(record.trialStartDate)} başlayacak.`;
  return record.trialDaysRemaining === null ? null : record.trialDaysRemaining === 0 ? 'Denemenin son günü' : `Denemenin bitmesine ${record.trialDaysRemaining} gün`;
}
export function accessHint(record: Pick<Subscription, 'status' | 'accessEndsOn' | 'context'>) {
  if (!record.accessEndsOn) return 'Erişim bitişi belirtilmedi.';
  if (record.status === 'Expired' || record.accessEndsOn < record.context.today) return `Erişim sona erdi · ${dateLabel(record.accessEndsOn, false, true)}`;
  return `${dateLabel(record.accessEndsOn, false, true)} dahil erişim ${record.status === 'Cancelled' ? 'devam ediyor' : 'bitişi'}.`;
}
export function SubscriptionRow({ record }: { record: Subscription }) {
  const trial = trialHint(record); const urgent = record.status === 'Trial' && record.trialStartDate !== null && record.trialStartDate <= record.context.today && record.trialDaysRemaining !== null && record.trialDaysRemaining <= 7;
  return <Pressable accessibilityRole="button" accessibilityLabel={`${record.name}, ${statusLabels[record.status]}, detay`} onPress={() => router.push({ pathname: '/subscription', params: { id: record.id } })} style={{ paddingVertical: 18, minHeight: 150, gap: 10 }}>
    <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}><View style={{ flex: 1 }}><AppText variant="section">{record.name}</AppText><AppText tone="muted" variant="caption">{record.providerName} · {frequencyLabels[record.billingFrequency]} / {record.billingInterval} dönem</AppText></View><StatusBadge label={statusLabels[record.status]} tone={record.status === 'Trial' ? 'warning' : record.status === 'Active' ? 'success' : 'muted'} /></View>
    <FinancialAmount amount={record.amount} currency={record.currencyCode} />
    <AppText variant="caption" tone="muted">{record.nextBillingDate ? `Sonraki ücretli tarih: ${dateLabel(record.nextBillingDate, false, true)}` : 'Yeni ücretli ödeme tarihi yok'}</AppText>
    {trial && <AppText variant="caption" tone={urgent ? 'warning' : 'muted'}>{trial}</AppText>}
    {(record.status === 'Cancelled' || record.status === 'Expired') && <AppText variant="caption" tone="muted">{accessHint(record)}</AppText>}
  </Pressable>;
}
export function SubscriptionTotals({ data }: { data: SubscriptionSummary }) {
  return <AppCard><AppText variant="caption" tone="muted">AYLIK TAHMİNİ KARŞILIK</AppText>{data.monthlyEquivalentTotalsByCurrency.length ? data.monthlyEquivalentTotalsByCurrency.map(total => <View key={total.currencyCode} style={{ gap: 6, marginTop: 16 }}><FinancialAmount amount={total.estimatedAmount} currency={total.currencyCode} large /><AppText variant="caption" tone="muted">{total.currencyCode} / ay</AppText></View>) : <AppText tone="muted" style={{ marginTop: 12 }}>Bu kapsamda maliyet tahmini yok.</AppText>}
    <AppText style={{ marginTop: 18 }}>{data.activeSubscriptionCount} aktif · {data.trialSubscriptionCount} deneme · {data.cancelledSubscriptionCount} iptal · {data.expiredSubscriptionCount} sona erdi</AppText>
    <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>Otomatik yenilenen aktif ve deneme kayıtları için tahmin; bu ayın kesin harcaması değildir. Aylık: tutar/aralık; yıllık: tutar/(12×aralık); haftalık: tutar×52/(12×aralık); günlük: tutar×365/(12×aralık). Para birimleri ayrı, grup sonucu 4 basamakta yuvarlanır.</AppText>
  </AppCard>;
}
export function SubscriptionLimitNotice() {
  return <AppCard><AppText variant="section">Aktif kayıt sınırına ulaştın</AppText><AppText tone="warning" style={{ marginVertical: 12 }}>Abonelik takibi ve bağlı aktif ödeme planı için ayrı limitler vardır. Mevcut kayıtlarını kontrol edebilir veya üyelik seçeneklerini inceleyebilirsin.</AppText><AppButton title="Üyelik seçeneklerini incele" variant="secondary" onPress={() => router.push('/premium')} /></AppCard>;
}
export function MutationFailure({ error, blocked, reload }: { error: unknown; blocked: boolean; reload: () => void }) {
  return <AppCard><AppText tone="danger" accessibilityRole="alert">{error instanceof ApiError ? error.message : 'İşlem tamamlanamadı. Güncel kaydı kontrol et.'}</AppText>{blocked && <View style={{ gap: 12, marginTop: 12 }}><AppText tone="muted" variant="caption">Kayıt değişmiş veya istek sonucu belirsiz olabilir. Yeniden göndermeden önce güncel kayıtları kontrol et.</AppText><AppButton title="Güncel kaydı yükle" variant="secondary" onPress={reload} /></View>}</AppCard>;
}
export function SubscriptionDashboardCard() {
  const summary = useSubscriptionSummary(); const trials = useExpiringTrials();
  return <><SectionHeader title="Aboneliklerin" action="Tümü" onPress={() => router.push('/(tabs)/subscriptions')} />
    {summary.isError ? <FinanceError error={summary.error} retry={() => { void summary.refetch(); }} /> : !summary.data ? <AppCard><AppText tone="muted">Abonelik özeti yükleniyor…</AppText></AppCard> : <AppCard><AppText variant="section">{summary.data.activeSubscriptionCount} aktif · {summary.data.trialSubscriptionCount} deneme</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>Abonelik ödemeleri finansal özete bağlı plan üzerinden zaten dahildir.</AppText>{summary.data.nextRenewal && <AppText style={{ marginTop: 12 }}>Sonraki yenileme: {summary.data.nextRenewal.name} · {summary.data.nextRenewal.nextBillingDate ? dateLabel(summary.data.nextRenewal.nextBillingDate) : 'Tarih yok'}</AppText>}<AppButton title={`${summary.data.expiringTrialsCount} yaklaşan deneme bitişi`} variant="secondary" onPress={() => router.push('/subscription-trials')} /></AppCard>}
    {trials.isError ? <FinanceError error={trials.error} retry={() => { void trials.refetch(); }} /> : trials.data?.length ? <AppCard style={{ marginTop: 12 }}>{trials.data.slice(0, 3).map(item => <View key={item.subscriptionId} style={{ marginVertical: 8 }}><AppText variant="label">{item.name} · {item.daysRemaining === 0 ? 'Son gün' : `${item.daysRemaining} gün kaldı`}</AppText><AppText variant="caption" tone="muted">İlk ücretli: {dateLabel(item.firstBillingDate, false, true)}</AppText></View>)}</AppCard> : null}
  </>;
}
