import { useRef, useState } from 'react';
import { View } from 'react-native';
import { isCancel } from 'axios';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppText, AppCard, AppButton, AppInput, LoadingSkeleton, StatusBadge } from '../../components/ui';
import { usePayment, useOccurrence, useOccurrenceMutation, useFinanceClock } from '../payments/queries';
import { FinancialAmount, FinanceError } from '../payments/components';
import type { Occurrence, OccurrenceStatus, PaymentPlan } from '../payments/contracts';
import { canonicalAmount } from '../payments/money';
import { dateLabel } from '../../utils/format';
import { ApiError } from '../../services/api-error';
import { statusLabel } from './shared';
export default function OccurrenceScreen({ id, date }: { id: string; date: string }) {
  const plan = usePayment(id); const occurrence = useOccurrence(id, date); const clock = useFinanceClock(); const [revision, setRevision] = useState(0);
  const reload = async () => { const result = await occurrence.refetch(); if (result.isSuccess) { await plan.refetch(); setRevision(r => r + 1); } };
  if (plan.isError || occurrence.isError || clock.isError) return <AppScreen><AppHeader title="Tek ödeme" back /><FinanceError error={plan.error ?? occurrence.error ?? clock.error} retry={() => { void reload(); void clock.refetch(); }} /></AppScreen>;
  if (!plan.data || !occurrence.data || !clock.data) return <AppScreen><LoadingSkeleton /></AppScreen>;
  return <OccurrenceBody key={revision} plan={plan.data} initial={occurrence.data} today={clock.data.context.today} reload={reload} />;
}
export function OccurrenceBody({ plan, initial, today, reload }: { plan: PaymentPlan; initial: Occurrence; today: string; reload: () => Promise<void> }) {
  const [record, setRecord] = useState(initial); const [amount, setAmount] = useState(initial.actualAmount ?? plan.amount); const mutation = useOccurrenceMutation();
  const busy = useRef(false); const [error, setError] = useState<unknown>(null); const [blocked, setBlocked] = useState(false); const [success, setSuccess] = useState(false);
  async function change(status: OccurrenceStatus) {
    if (busy.current || blocked) return;
    if (status === 'Paid' && !canonicalAmount(amount)) { setError(new ApiError('Pozitif, en fazla 4 ondalık basamaklı gerçek tutarı gir.')); return; }
    busy.current = true; setError(null); setSuccess(false);
    try { const result = await mutation.mutateAsync({ id: plan.id, date: record.dueDate, status, rowVersion: record.rowVersion, amount }); setRecord(result); setSuccess(true); }
    catch (failure) { if (!isCancel(failure)) { setError(failure); if (failure instanceof ApiError && (failure.status === 409 || failure.kind === 'network' || failure.kind === 'timeout')) setBlocked(true); } }
    finally { busy.current = false; }
  }
  return <AppScreen keyboard><AppHeader title="Tek ödeme" subtitle={plan.name} back /><View style={{ gap: 16 }}>
    <AppCard><AppText tone="muted">Planlanan tarih</AppText><AppText variant="section">{dateLabel(record.dueDate, true, true)}</AppText><View style={{ marginVertical: 14 }}><StatusBadge label={statusLabel(record.status, record.dueDate, today)} tone={record.status === 'Paid' ? 'success' : record.status === 'Skipped' ? 'muted' : 'warning'} /></View><AppText tone="muted">Plan tutarı · güncel tanım</AppText><FinancialAmount amount={plan.amount} currency={plan.currencyCode} /><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>Geçmiş plan tutarı snapshot olarak saklanmıyor. Bu tutar plan düzenlendiğinde değişebilir. Geçmiş ödemenin para birimi güncel plana aittir; tarihsel para birimi saklanmıyor.</AppText>
      {record.actualAmount !== null && <View style={{ marginTop: 16 }}><AppText tone="muted">Gerçek ödenen</AppText><FinancialAmount amount={record.actualAmount} currency={plan.currencyCode} /></View>}
      {record.paidAtUtc && <AppText style={{ marginTop: 12 }}>Ödeme zamanı: {new Date(record.paidAtUtc).toLocaleString('tr-TR')}</AppText>}
    </AppCard>
    <AppButton title="Ödeme planını gör" variant="secondary" onPress={() => router.push({ pathname: '/payment', params: { id: plan.id } })} />
    {plan.isActive ? <><AppInput label="Gerçek ödenen tutar" value={amount} onChangeText={setAmount} keyboardType="decimal-pad" editable={!mutation.isPending && !blocked} /><AppText variant="caption" tone="muted">Ödeme zamanı sunucu tarafından işlem anında UTC kaydedilir. Geçmiş bir ödeme zamanı seçmek bu API ile desteklenmiyor. Bu işlem bankadan tahsilat yapmaz.</AppText>
      <AppButton title="Ödendi olarak işaretle" loading={mutation.isPending} disabled={blocked} onPress={() => { void change('Paid'); }} /><AppButton title="Atlandı olarak işaretle" variant="secondary" disabled={mutation.isPending || blocked} onPress={() => { void change('Skipped'); }} /><AppButton title="Yeniden bekliyor" variant="ghost" disabled={mutation.isPending || blocked || !record.isRecorded} onPress={() => { void change('Pending'); }} /></> : <AppText tone="muted">Pasif planın geçmişi salt okunurdur.</AppText>}
    {success && <AppText accessibilityRole="alert" tone="success">Ödeme durumu sunucuda güncellendi.</AppText>}
    {!!error && <AppCard><AppText tone="danger" accessibilityRole="alert">{error instanceof ApiError ? error.message : 'İşlem tamamlanamadı.'}</AppText>{blocked && <><AppText variant="caption" tone="muted" style={{ marginVertical: 12 }}>Kayıt değişmiş veya istek sonucu belirsiz olabilir. Yeniden göndermeden önce güncel durumu yükle.</AppText><AppButton title="Güncel durumu yükle" onPress={() => { void reload(); }} variant="secondary" /></>}</AppCard>}
  </View></AppScreen>;
}
