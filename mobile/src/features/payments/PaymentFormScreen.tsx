import { useRef, useState } from 'react';
import { Switch, View } from 'react-native';
import { router } from 'expo-router';
import { isCancel } from 'axios';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AppScreen, AppHeader, AppInput, AppButton, AppText, AppCard, LoadingSkeleton, styles } from '../../components/ui';
import { ApiError } from '../../services/api-error';
import { useFinanceClock, usePayment, usePaymentMutation, useEntitlements } from './queries';
import { FinanceError, LimitNotice } from './components';
import { paymentFormSchema, formDefaults, toPaymentWrite, frequencyLabels, type PaymentFields } from './validation';
import { DateField } from '../../components/date-field';
import type { PaymentPlan, Entitlement } from './contracts';

export default function PaymentFormScreen({ id }: { id?: string }) {
  const query = usePayment(id); const clock = useFinanceClock(); const entitlements = useEntitlements(); const [revision, setRevision] = useState(0);
  if (clock.isError || (id && query.isError)) return <AppScreen><AppHeader title={id ? 'Planı düzenle' : 'Yeni ödeme'} back /><FinanceError error={clock.error ?? query.error} retry={() => { void clock.refetch(); if (id) void query.refetch(); }} /></AppScreen>;
  if (!clock.data || (id && !query.data)) return <AppScreen><LoadingSkeleton /></AppScreen>;
  return <PaymentForm key={`${id ?? 'new'}-${revision}`} plan={id ? query.data : undefined} today={clock.data.context.today} activeCount={clock.data.activePaymentPlanCount} entitlement={entitlements.data} entitlementError={entitlements.isError} reload={async () => { const result = await query.refetch(); if (result.isSuccess) setRevision((value) => value + 1); }} />;
}
export function PaymentForm({ plan, today, activeCount, entitlement, entitlementError, reload }: { plan?: PaymentPlan; today: string; activeCount: number; entitlement?: Entitlement; entitlementError?: boolean; reload: () => Promise<void> }) {
  // Freeze the version being edited. Background refetch must not silently replace it.
  const [original] = useState(plan); const busy = useRef(false); const mutation = usePaymentMutation();
  const [failure, setFailure] = useState<unknown>(null); const [conflict, setConflict] = useState(false); const [uncertain, setUncertain] = useState(false);
  const { control, setValue, setError, handleSubmit, formState: { errors } } = useForm<PaymentFields>({ resolver: zodResolver(paymentFormSchema), defaultValues: formDefaults(today, original) });
  const startDate = useWatch({ control, name: 'startDate' });
  const frequency = useWatch({ control, name: 'frequency' }); const active = useWatch({ control, name: 'isActive' });
  const limitReached = !!entitlement && activeCount >= entitlement.limits.activePaymentPlans && !original?.isActive;
  const submit = () => handleSubmit(async (fields) => {
    if (busy.current || conflict || uncertain) return; busy.current = true; setFailure(null);
    try {
      const body = toPaymentWrite(fields);
      const result = await mutation.mutateAsync(original ? { kind: 'update', id: original.id, rowVersion: original.rowVersion, body } : { kind: 'create', body });
      if (result) {
        const target = { pathname: '/payment' as const, params: { id: result.id } };
        if (original) router.dismissTo(target); else router.replace(target);
      }
    } catch (error) {
      if (!isCancel(error)) {
        setFailure(error);
        if (error instanceof ApiError) {
          if (error.status === 409) setConflict(true);
          if (error.kind === 'network' || error.kind === 'timeout') setUncertain(true);
          for (const name of Object.keys(formDefaults(today)) as (keyof PaymentFields)[]) {
            const messages = error.errors?.[name] ?? error.errors?.[name[0].toUpperCase() + name.slice(1)];
            if (Array.isArray(messages) && messages.length) setError(name, { type: 'server', message: messages.join(' ') });
          }
        }
      }
    } finally { busy.current = false; }
  })();
  const input = (name: Exclude<keyof PaymentFields, 'isActive' | 'frequency'>, label: string, options: { numeric?: boolean; placeholder?: string; maxLength?: number; multiline?: boolean } = {}) => <Controller key={name} control={control} name={name} render={({ field }) => <AppInput label={label} value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors[name]?.message} placeholder={options.placeholder} maxLength={options.maxLength} multiline={options.multiline} keyboardType={name === 'amount' ? 'decimal-pad' : options.numeric ? 'number-pad' : 'default'} autoCapitalize={name === 'currencyCode' ? 'characters' : 'sentences'} editable={!mutation.isPending && !conflict} />} />;
  return <AppScreen keyboard><AppHeader title={original ? 'Planı düzenle' : 'Yeni ödeme'} subtitle="Gelecek günlerine yer aç." back />
    {entitlement && <AppText variant="caption" tone="muted" style={{ marginBottom: 16 }}>{entitlement.plan} · {activeCount}/{entitlement.limits.activePaymentPlans} aktif plan</AppText>}
    {(limitReached || failure instanceof ApiError && failure.code === 'entitlement_limit') && <LimitNotice />}
    {entitlementError && <AppText tone="warning" variant="caption" style={{ marginBottom: 16 }}>Üyelik limiti alınamadı. Kaydetme sırasında sunucu sınırı kontrol edecek.</AppText>}
    <View style={{ gap: 20 }}>
      {input('name', 'Ödeme adı', { maxLength: 150, placeholder: 'Örn. İnternet faturası' })}
      {input('amount', 'Tutar', { placeholder: '899,90' })}
      {input('currencyCode', 'Para birimi · ISO 4217', { maxLength: 3, placeholder: 'TRY / USD / EUR' })}
      {input('category', 'Kategori', { maxLength: 50, placeholder: 'Örn. Ev ve yaşam' })}
      <AppText variant="label">Tekrarlama</AppText>
      <Controller control={control} name="frequency" render={({ field }) => <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>{(Object.keys(frequencyLabels) as (keyof typeof frequencyLabels)[]).map((value) => <View key={value} style={{ minWidth: '45%', flexGrow: 1 }}><AppButton title={frequencyLabels[value] + (value === field.value ? ' ✓' : '')} variant={value === field.value ? 'primary' : 'secondary'} disabled={mutation.isPending || conflict} onPress={() => { field.onChange(value); if (!['Monthly', 'Yearly'].includes(value)) setValue('anchorDay', ''); }} /></View>)}</View>} />
      {frequency !== 'Once' && input('interval', 'Tekrar aralığı', { numeric: true, placeholder: '1' })}
      <Controller control={control} name="startDate" render={({ field }) => <DateField label="İlk ödeme tarihi" value={field.value} onChange={field.onChange} fallback={today} disabled={mutation.isPending || conflict} error={errors.startDate?.message} />} />
      <Controller control={control} name="endDate" render={({ field }) => <DateField label="Bitiş tarihi · isteğe bağlı" value={field.value} onChange={field.onChange} fallback={startDate || today} minimum={startDate} optional disabled={mutation.isPending || conflict} error={errors.endDate?.message} />} />
      {['Monthly', 'Yearly'].includes(frequency) && <>{input('anchorDay', 'Tekrar günü · isteğe bağlı', { numeric: true, maxLength: 2, placeholder: 'Başlangıç günü kullanılır' })}<AppCard><AppText variant="caption" tone="muted">İlk ödeme her zaman başlangıç tarihidir. Sonraki dönemlerde seçilen gün ayda yoksa ayın son günü kullanılır. 31 Ocak sonrası 28/29 Şubat, 29 Şubat yıllık tekrarında 28 Şubat uygulanır.</AppText></AppCard></>}
      {input('description', 'Açıklama · isteğe bağlı', { maxLength: 2000, multiline: true })}
      <Controller control={control} name="isActive" render={({ field }) => <View style={[styles.row, { minHeight: 60 }]}><View style={{ flex: 1 }}><AppText variant="label">Aktif plan</AppText><AppText variant="caption" tone="muted">Pasif plan yeni bekleyen tarih üretmez.</AppText></View><Switch accessibilityLabel="Aktif plan" value={field.value} disabled={mutation.isPending || conflict} onValueChange={field.onChange} style={{ minHeight: 48 }} /></View>} />
      {!!failure && <AppCard><AppText accessibilityRole="alert" tone="danger">{failure instanceof ApiError ? failure.message : 'İşlem tamamlanamadı. Kaydın oluşup oluşmadığını kontrol edip yeniden dene.'}</AppText>{conflict && <View style={{ marginTop: 12 }}><AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>Yeni sürümün üzerine yazılmadı. Güncel kaydı yüklemek formdaki değişiklikleri kaldırır. Aboneliğe bağlı planlar bu ekrandan değiştirilemeyebilir.</AppText><AppButton title="Güncel kaydı yükle" variant="secondary" onPress={() => { void reload(); }} /></View>}</AppCard>}
      {uncertain && <AppCard><AppText tone="warning">Sonuç doğrulanamadı. Tekrar göndermeden önce planlarını kontrol et; sunucu kaydı tamamlamış olabilir.</AppText><View style={{ marginTop: 12, gap: 12 }}><AppButton title="Planlarımı kontrol et" variant="secondary" onPress={() => router.push('/(tabs)/payments')} /><AppButton title="Kontrol ettim, yeniden göndereceğim" variant="ghost" onPress={() => setUncertain(false)} /></View></AppCard>}
      <AppButton title={original ? 'Değişiklikleri kaydet' : 'Ödeme planını oluştur'} loading={mutation.isPending} disabled={mutation.isPending || conflict || uncertain || active && limitReached} onPress={submit} />
      <AppText variant="caption" tone="muted">Plan oluşturulur; banka hesabından tahsilat yapılmaz.</AppText>
    </View>
  </AppScreen>;
}
