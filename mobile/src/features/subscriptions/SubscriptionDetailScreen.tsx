import { useRef, useState } from 'react';
import { Alert, View } from 'react-native';
import { router } from 'expo-router';
import { isCancel } from 'axios';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, LoadingSkeleton, StatusBadge } from '../../components/ui';
import { DateField } from '../../components/date-field';
import { FinancialAmount, FinanceError } from '../payments/components';
import { frequencyLabels } from '../payments/validation';
import { validDate } from '../payments/dates';
import { ApiError } from '../../services/api-error';
import { dateLabel } from '../../utils/format';
import { useSubscription, useSubscriptionMutation } from './queries';
import { statusLabels, trialHint, accessHint, MutationFailure, SubscriptionLimitNotice } from './components';
import type { Subscription } from './contracts';
import { CatalogIdentity } from '../catalog/CatalogIdentity';
export default function SubscriptionDetailScreen({ id }: { id: string }) {
  const query = useSubscription(id); const [revision, setRevision] = useState(0);
  const reload = async () => { const result = await query.refetch(); if (result.isSuccess) setRevision(n => n + 1); };
  if (query.isError) return <AppScreen><AppHeader title="Abonelik detayı" back /><FinanceError error={query.error} retry={() => { void reload(); }} /></AppScreen>;
  if (!query.data) return <AppScreen><LoadingSkeleton /></AppScreen>;
  return <SubscriptionDetail key={revision} initial={query.data} reload={reload} />;
}
export function SubscriptionDetail({ initial, reload }: { initial: Subscription; reload: () => Promise<void> }) {
  const [record, setRecord] = useState(initial); const [accessEnd, setAccessEnd] = useState(initial.accessEndsOn && initial.accessEndsOn >= initial.context.today ? initial.accessEndsOn : '');
  const mutation = useSubscriptionMutation(); const busy = useRef(false); const [failure, setFailure] = useState<unknown>(null); const [blocked, setBlocked] = useState(false); const [success, setSuccess] = useState(false);
  const mutable = ['Active', 'Trial'].includes(record.status); const minimum = record.startDate > record.context.today ? record.startDate : record.context.today;
  async function change(kind: 'cancel' | 'delete') {
    if (busy.current || blocked) return; busy.current = true; setFailure(null); setSuccess(false);
    try {
      const result = await mutation.mutateAsync(kind === 'cancel' ? { kind, id: record.id, rowVersion: record.rowVersion, accessEndsOn: accessEnd } : { kind, id: record.id, rowVersion: record.rowVersion });
      if (kind === 'delete') router.dismissTo('/(tabs)/subscriptions'); else if (result) { setRecord(result); setSuccess(true); }
    } catch (error) { if (!isCancel(error)) { setFailure(error); if (error instanceof ApiError && (error.status === 409 || error.kind === 'network' || error.kind === 'timeout')) setBlocked(true); } }
    finally { busy.current = false; }
  }
  const date = (value: string | null) => value ? dateLabel(value, true, true) : 'Belirtilmedi';
  const hint = trialHint(record);
  return <AppScreen refreshing={false} onRefresh={() => { void reload(); }}><AppHeader title="Abonelik detayı" subtitle={record.providerName} back /><View style={{ gap: 18 }}>
    <AppCard><StatusBadge label={statusLabels[record.status]} tone={record.status === 'Trial' ? 'warning' : record.status === 'Active' ? 'success' : 'muted'} /><AppText variant="title" style={{ marginTop: 16 }}>{record.name}</AppText><View style={{ marginVertical: 18 }}><FinancialAmount amount={record.amount} currency={record.currencyCode} large /></View>
      <Detail label="Faturalandırma" value={`${frequencyLabels[record.billingFrequency]} · Her ${record.billingInterval} dönem`} /><Detail label="Abonelik başlangıcı" value={date(record.startDate)} /><Detail label="İlk ücretli ödeme" value={date(record.firstBillingDate)} /><Detail label="Sonraki ücretli ödeme" value={date(record.nextBillingDate)} /><Detail label="Deneme başlangıcı" value={date(record.trialStartDate)} /><Detail label="Deneme bitişi" value={date(record.trialEndDate)} />
      {hint && <AppText tone="warning" style={{ marginTop: 14 }}>{hint}</AppText>}<Detail label="Otomatik yenileme takibi" value={record.autoRenew ? 'Açık' : 'Kapalı'} /><Detail label="Erişim bitişi" value={date(record.accessEndsOn)} />
      <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{accessHint(record)}</AppText>{record.cancelledAtUtc && <Detail label="Takip iptal zamanı" value={new Date(record.cancelledAtUtc).toLocaleString('tr-TR')} />}
      <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>Hesap tarihi: {date(record.context.today)} · {record.context.timeZoneId}{record.context.usedTimeZoneFallback ? ' · UTC yedeği kullanıldı' : ''}</AppText>
      {record.description && <AppText style={{ marginTop: 16 }}>{record.description}</AppText>}
    </AppCard>
    <AppButton title="Bağlı ödeme planını gör" variant="secondary" onPress={() => router.push({ pathname: '/payment', params: { id: record.paymentPlanId, readOnly: 'true' } })} />
    <CatalogIdentity categoryId={record.catalogCategoryId} serviceId={record.catalogServiceId} fallback={record.category ?? 'Özel abonelik'} />
    <AppButton title="Bağlı planın ödeme hatırlatmaları" variant="secondary" onPress={() => router.push({ pathname: '/reminders', params: { id: record.paymentPlanId } })} />
    <AppCard><AppText variant="label">Deneme hatırlatmaları: {record.enableTrialReminders ? 'Tercih açık' : 'Tercih kapalı'}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>Sunucu deneme kurallarını üyelik kotasına göre 09:00 hesap saatinde oluşturur: Free için 1 gün, uygun Premium kotasında 1/3/7 gün ve son gün. Özel deneme günü/saat seçimi desteklenmez. Tercihi aboneliği düzenleyerek değiştirebilirsin; iptal/sona ermede gönderim uygunluğu sunucuda kontrol edilir. Tercih, cihaz teslimi değildir.</AppText></AppCard>
    <AppText variant="caption" tone="muted">Bağlı ödeme planı finansal tek kaynaktır; takvim ve analizde ayrıca ikinci abonelik tutarı eklenmez.</AppText>
    {mutable ? <><AppButton title="Aboneliği düzenle" disabled={mutation.isPending || blocked} onPress={() => router.push({ pathname: '/edit-subscription', params: { id: record.id } })} />
      <AppCard><AppText variant="section">İptal ve erişim</AppText><AppText tone="muted" style={{ marginVertical: 14 }}>PayAhead takip kaydını iptal etmek dış hizmetteki aboneliği iptal etmez. Sağlayıcında ayrıca iptal etmelisin. Gerçek erişim bitişini aşağıda belirt.</AppText><DateField label="İptal sonrası erişimin son günü" value={accessEnd} onChange={setAccessEnd} fallback={minimum} minimum={minimum} disabled={mutation.isPending || blocked} />
        <View style={{ marginTop: 16 }}><AppButton title="Takip kaydını iptal et" variant="secondary" disabled={mutation.isPending || blocked || !validDate(accessEnd) || accessEnd < minimum} onPress={() => Alert.alert('Takip kaydı iptal edilsin mi?', `Dış sağlayıcıda iptal yapılmaz. PayAhead yenilemeleri durur; ${dateLabel(accessEnd, true, true)} dahil erişim devam ediyor olarak izlenir.`, [{ text: 'Vazgeç', style: 'cancel' }, { text: 'Takibi iptal et', onPress: () => { void change('cancel'); } }])} /></View></AppCard></> : <AppCard><AppText tone="muted">İptal edilmiş veya sona ermiş kayıt yeniden etkinleştirilemez. Gerekirse yeni bir takip kaydı oluştur.</AppText></AppCard>}
    <AppButton title="Takip kaydını kaldır" variant="ghost" disabled={mutation.isPending || blocked} onPress={() => Alert.alert('Takip kaydı kaldırılsın mı?', 'Abonelik listeden gizlenir ve bağlı plan pasifleştirilir. Ödeme geçmişi korunur; dış sağlayıcıda iptal yapılmaz.', [{ text: 'Vazgeç', style: 'cancel' }, { text: 'Kaldır', style: 'destructive', onPress: () => { void change('delete'); } }])} />
    {mutation.isPending && <AppText accessibilityLiveRegion="polite">İşlem sürüyor…</AppText>}{success && <AppText accessibilityRole="alert" tone="success">Takip kaydı iptal edildi. Dış sağlayıcıda ayrıca işlem yapmalısın.</AppText>}
    {failure instanceof ApiError && failure.code === 'entitlement_limit' && <SubscriptionLimitNotice />}{!!failure && <MutationFailure error={failure} blocked={blocked} reload={() => { void reload(); }} />}
  </View></AppScreen>;
}
function Detail({ label, value }: { label: string; value: string }) { return <View style={{ gap: 4, marginTop: 12 }}><AppText tone="muted" variant="caption">{label}</AppText><AppText variant="label">{value}</AppText></View>; }
