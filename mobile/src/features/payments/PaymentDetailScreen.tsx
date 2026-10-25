import { CatalogIdentity } from '../catalog/CatalogIdentity';
import { useRef, useState } from 'react';
import { Alert, View } from 'react-native';
import { router } from 'expo-router';
import { isCancel } from 'axios';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, StatusBadge, LoadingSkeleton, styles } from '../../components/ui';
import { ApiError } from '../../services/api-error';
import { usePayment, usePaymentMutation, useFinanceClock } from './queries';
import { FinancialAmount, FinanceError } from './components';
import { dateLabel } from '../../utils/format';
import { nextPlanDate } from './dates';
import { frequencyLabels } from './validation';
import type { PaymentWrite } from './contracts';

export default function PaymentDetailScreen({ id, readOnly = false }: { id: string; readOnly?: boolean }) {
  const query = usePayment(id); const clock = useFinanceClock(); const mutation = usePaymentMutation(); const busy = useRef(false); const [failure, setFailure] = useState<unknown>(null);
  const plan = query.data; const next = plan && clock.data ? nextPlanDate(plan, clock.data.context.today) : undefined;
  async function change(kind: 'delete' | 'deactivate') {
    if (!plan || busy.current) return; busy.current = true; setFailure(null);
    try {
      if (kind === 'delete') { await mutation.mutateAsync({ kind: 'delete', id: plan.id, rowVersion: plan.rowVersion }); router.dismissTo('/(tabs)/payments'); }
      else { const body: PaymentWrite = { name: plan.name, description: plan.description, amount: plan.amount, currencyCode: plan.currencyCode, category: plan.category, frequency: plan.frequency, interval: plan.interval, startDate: plan.startDate, endDate: plan.endDate, anchorDay: plan.anchorDay, isActive: false }; await mutation.mutateAsync({ kind: 'update', id: plan.id, rowVersion: plan.rowVersion, body }); }
    } catch (error) { if (!isCancel(error)) setFailure(error); } finally { busy.current = false; }
  }
  const blocked = mutation.isPending || failure instanceof ApiError && failure.status === 409;
  return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch().then((result) => { if (result.isSuccess) setFailure(null); }); }}><AppHeader title="Ödeme detayı" back />
    {query.isPending ? <LoadingSkeleton /> : query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }} /> : plan ? <>
      <AppCard><StatusBadge label={plan.isActive ? 'Aktif plan' : 'Pasif plan'} /><AppText variant="title" style={{ marginTop: 18 }}>{plan.name}</AppText><View style={{ marginVertical: 20 }}><FinancialAmount amount={plan.amount} currency={plan.currencyCode} large /></View>
        <Detail label="Kategori" value={plan.category} /><Detail label="Tekrar" value={`${frequencyLabels[plan.frequency]} · Her ${plan.interval} dönem`} /><Detail label="İlk ödeme" value={dateLabel(plan.startDate, true, true)} /><Detail label="Bitiş" value={plan.endDate ? dateLabel(plan.endDate, true, true) : 'Belirtilmedi'} /><Detail label="Tekrar günü" value={plan.anchorDay === null ? 'Başlangıç günü' : String(plan.anchorDay)} /><Detail label="Planlanan" value={next === undefined ? 'Hesap tarihi alınamadı' : next ? dateLabel(next, true, true) : 'Yeni tarih yok'} />
        <AppText variant="caption" tone="muted" style={{ marginTop: 14 }}>Planlanan tarih tekrar takvimidir; ödenen/atlanan durumunu içermez.</AppText>{plan.description && <AppText tone="muted" style={{ marginTop: 20 }}>{plan.description}</AppText>}
      </AppCard>
      <CatalogIdentity categoryId={plan.catalogCategoryId} serviceId={plan.catalogServiceId} fallback={plan.category} /><View style={{ marginTop: 16 }}><AppButton title="Ödeme hatırlatmalarını yönet" variant="secondary" onPress={() => router.push({ pathname: '/reminders', params: { id: plan.id } })} /></View>
      {readOnly ? <AppCard style={{ marginTop: 18 }}><AppText tone="muted">Bağlı plan abonelik takip kaydı üzerinden yönetilir. Değişiklikler için abonelik detayına geri dön.</AppText></AppCard> : <View style={{ gap: 12, marginTop: 24 }}><AppButton title="Planı düzenle" disabled={blocked} onPress={() => router.push({ pathname: '/edit-payment', params: { id: plan.id } })} />
        {plan.isActive && <AppButton title="Planı pasifleştir" variant="secondary" disabled={blocked} onPress={() => Alert.alert('Plan pasifleştirilsin mi?', 'Yeni bekleyen tarihler üretilmez. Mevcut ödeme geçmişi korunur.', [{ text: 'Vazgeç', style: 'cancel' }, { text: 'Pasifleştir', onPress: () => { void change('deactivate'); } }])} />}
        <AppButton title="Planı sil" variant="ghost" disabled={blocked} onPress={() => Alert.alert('Ödeme planı silinsin mi?', `${plan.name} kalıcı olarak silinecek. Geçmiş/hatırlatma veya abonelik bağlantısı varsa sunucu işlemi reddedebilir.`, [{ text: 'Vazgeç', style: 'cancel' }, { text: 'Sil', style: 'destructive', onPress: () => { void change('delete'); } }])} />
      </View>}
      {!!failure && <AppCard style={{ marginTop: 20 }}><AppText tone="danger" accessibilityRole="alert">{failure instanceof ApiError ? failure.message : 'İşlem tamamlanamadı. Güncel kaydı kontrol et.'}</AppText><View style={{ marginTop: 12 }}><AppButton title="Güncel kaydı yükle" variant="secondary" onPress={() => { void query.refetch().then((result) => { if (result.isSuccess) setFailure(null); }); }} /></View></AppCard>}
    </> : null}
  </AppScreen>;
}
function Detail({ label, value }: { label: string; value: string }) { return <View style={[styles.row, { gap: 14, alignItems: 'flex-start', marginTop: 14 }]}><AppText tone="muted" style={{ flex: 1 }}>{label}</AppText><AppText variant="label" style={{ flex: 1, textAlign: 'right' }}>{value}</AppText></View>; }
