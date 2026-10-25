import { useRef, useState } from 'react';
import { Switch, View } from 'react-native';
import { router } from 'expo-router';
import { isCancel } from 'axios';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AppScreen, AppHeader, AppText, AppInput, AppButton, AppCard, LoadingSkeleton, styles } from '../../components/ui';
import { DateField } from '../../components/date-field';
import { FinanceError } from '../payments/components';
import { useEntitlements, useFinanceClock } from '../payments/queries';
import type { Entitlement } from '../payments/contracts';
import { ApiError } from '../../services/api-error';
import { useSubscription, useSubscriptionMutation, useSubscriptionSummary } from './queries';
import { subscriptionDefaults, subscriptionFormSchema, toSubscriptionWrite, consumesTrackingSlot, billingLabels, type SubscriptionFields } from './validation';
import type { Subscription } from './contracts';
import { CatalogSelectionField } from '../catalog/CatalogPicker';
import { SubscriptionLimitNotice, MutationFailure } from './components';

export default function SubscriptionFormScreen({ id }: { id?: string }) {
  const detail = useSubscription(id); const summary = useSubscriptionSummary(); const clock = useFinanceClock(); const entitlement = useEntitlements(); const [revision, setRevision] = useState(0);
  const reload = async () => { if (!id) { router.dismissTo('/(tabs)/subscriptions'); return; } const result = await detail.refetch(); if (result.isSuccess) setRevision(n => n + 1); };
  if (summary.isError || clock.isError || id && detail.isError) return <AppScreen><AppHeader title="Abonelik" back /><FinanceError error={summary.error ?? clock.error ?? detail.error} retry={() => { void summary.refetch(); void clock.refetch(); if (id) void detail.refetch(); }} /></AppScreen>;
  if (!summary.data || !clock.data || id && !detail.data) return <AppScreen><LoadingSkeleton /></AppScreen>;
  if (id && detail.data && !['Active', 'Trial'].includes(detail.data.status)) return <AppScreen><AppHeader title="Abonelik düzenleme" back /><AppCard><AppText>İptal edilmiş veya sona ermiş takip kaydı düzenlenemez.</AppText></AppCard></AppScreen>;
  return <SubscriptionForm key={`${id ?? 'new'}-${revision}`} record={id ? detail.data : undefined} today={summary.data.context.today} trackingCount={summary.data.activeSubscriptionCount + summary.data.trialSubscriptionCount} paymentCount={clock.data.activePaymentPlanCount} entitlement={entitlement.data} reload={reload} />;
}
export function SubscriptionForm({ record, today, trackingCount, paymentCount, entitlement, reload }: { record?: Subscription; today: string; trackingCount: number; paymentCount: number; entitlement?: Entitlement; reload: () => Promise<void> }) {
  const [original] = useState(record); const busy = useRef(false); const mutation = useSubscriptionMutation(); const [failure, setFailure] = useState<unknown>(null); const [blocked, setBlocked] = useState(false);
  const { control, setValue, setError, handleSubmit, formState: { errors } } = useForm<SubscriptionFields>({ resolver: zodResolver(subscriptionFormSchema), defaultValues: subscriptionDefaults(today, original) });
  const values = useWatch({ control }); const status = values.status; const frequency = values.billingFrequency;
  const [catalogError, setCatalogError] = useState('');
  const needsTracking = !original && consumesTrackingSlot(values as SubscriptionFields, today);
  const trackingLimit = !!entitlement && needsTracking && trackingCount >= entitlement.limits.activeTrackedSubscriptions;
  const paymentLimit = !!entitlement && !!values.autoRenew && !original?.autoRenew && paymentCount >= entitlement.limits.activePaymentPlans;
  const submit = () => handleSubmit(async fields => {
    if (busy.current || blocked || trackingLimit || paymentLimit) return;
    if (!original && !fields.catalogCategoryId) { setCatalogError('Bir kategori seç. Katalogda yoksa özel abonelik oluşturabilirsin.'); return; }
    busy.current = true; setFailure(null);
    try {
      const body = toSubscriptionWrite(fields); const result = await mutation.mutateAsync(original ? { kind: 'update', id: original.id, rowVersion: original.rowVersion, body } : { kind: 'create', body });
      if (result) { const target = { pathname: '/subscription' as const, params: { id: result.id } }; if (original) router.dismissTo(target); else router.replace(target); }
    } catch (error) {
      if (!isCancel(error)) {
        setFailure(error);
        if (error instanceof ApiError) {
          if (error.status === 409 || error.kind === 'network' || error.kind === 'timeout') setBlocked(true);
          for (const name of Object.keys(subscriptionDefaults(today)) as (keyof SubscriptionFields)[]) {
            const messages = error.errors?.[name] ?? error.errors?.[name[0].toUpperCase() + name.slice(1)];
            if (messages?.length) setError(name, { type: 'server', message: messages.join(' ') });
          }
        }
      }
    } finally { busy.current = false; }
  })();
  const disabled = mutation.isPending || blocked;
  const input = (name: 'name' | 'providerName' | 'amount' | 'currencyCode' | 'billingInterval' | 'anchorDay' | 'description', label: string, maxLength?: number) => <Controller key={name} control={control} name={name} render={({ field }) => <AppInput label={label} value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors[name]?.message} editable={!disabled} maxLength={maxLength} keyboardType={name === 'amount' ? 'decimal-pad' : ['billingInterval', 'anchorDay'].includes(name) ? 'number-pad' : 'default'} autoCapitalize={name === 'currencyCode' ? 'characters' : 'sentences'} multiline={name === 'description'} />} />;
  const date = (name: 'startDate' | 'firstBillingDate' | 'trialStartDate' | 'trialEndDate' | 'accessEndsOn', label: string, minimum?: string) => <Controller key={name} control={control} name={name} render={({ field }) => <DateField label={label} value={field.value} onChange={field.onChange} fallback={values.startDate || today} minimum={minimum} disabled={disabled} error={errors[name]?.message} />} />;
  const toggle = (name: 'autoRenew' | 'enableTrialReminders', label: string) => <Controller control={control} name={name} render={({ field }) => <View style={[styles.row, { gap: 12, minHeight: 56 }]}><AppText variant="label" style={{ flex: 1 }}>{label}</AppText><Switch accessibilityLabel={label} value={field.value} onValueChange={field.onChange} disabled={disabled} /></View>} />;
  return <AppScreen keyboard><AppHeader title={original ? 'Aboneliği düzenle' : 'Yeni abonelik'} subtitle="Düzenli giderlerine net bir bakış." back /><View style={{ gap: 20 }}>
    {entitlement ? <AppText variant="caption" tone="muted">{entitlement.plan} · {trackingCount}/{entitlement.limits.activeTrackedSubscriptions} aktif/deneme · {paymentCount}/{entitlement.limits.activePaymentPlans} aktif ödeme planı</AppText> : <AppText variant="caption" tone="muted">Üyelik limitini sunucu kaydederken kontrol edecek.</AppText>}
    {(trackingLimit || paymentLimit || failure instanceof ApiError && failure.code === 'entitlement_limit') && <SubscriptionLimitNotice />}
    <CatalogSelectionField mode="Subscription" initialOpen={!original} categoryId={values.catalogCategoryId} serviceId={values.catalogServiceId} error={catalogError || errors.catalogCategoryId?.message || errors.catalogServiceId?.message} disabled={disabled} onSelect={selection => { setCatalogError(''); setValue('catalogCategoryId', selection.category.id); setValue('catalogServiceId', selection.service?.id ?? null); if (selection.service) { setValue('name', selection.service.displayName); setValue('providerName', selection.service.displayName.slice(0, 100)); const f = selection.service.defaultRecurrenceType; if (f && f !== 'Once') setValue('billingFrequency', f); } else if (values.catalogServiceId) { setValue('name', ''); setValue('providerName', ''); } }} />
    {!values.catalogServiceId || original ? <>{input('name', 'Abonelik adı', 150)}{input('providerName', 'Sağlayıcı adı', 100)}</> : <AppText variant="section">{values.name}</AppText>}{input('amount', 'Abonelik tutarı')}{input('currencyCode', 'Para birimi · ISO 4217', 3)}
    <AppText variant="label">Faturalandırma sıklığı</AppText><Controller control={control} name="billingFrequency" render={({ field }) => <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>{(Object.keys(billingLabels) as (keyof typeof billingLabels)[]).map(value => <View key={value} style={{ flexGrow: 1, minWidth: '45%' }}><AppButton title={billingLabels[value] + (field.value === value ? ' ✓' : '')} variant={field.value === value ? 'primary' : 'secondary'} disabled={disabled} onPress={() => { field.onChange(value); if (!['Monthly', 'Yearly'].includes(value)) setValue('anchorDay', ''); }} /></View>)}</View>} />
    {input('billingInterval', 'Faturalandırma aralığı')}{date('startDate', 'Abonelik başlangıcı')}{date('firstBillingDate', 'İlk ücretli ödeme tarihi', values.startDate)}
    <AppText variant="caption" tone="muted">Sonraki ücretli tarih sunucu tarafından hesaplanır. İlk ücretli tarih deneme bitişiyle aynı değildir.</AppText>
    {['Monthly', 'Yearly'].includes(frequency ?? '') && <>{input('anchorDay', 'Tekrar günü · isteğe bağlı', 2)}<AppText variant="caption" tone="muted">Gün ayda yoksa ay sonu kullanılır. İlk ücretli tarih ilk tekrardır; sonraki aylar seçilen güne göre ilerler.</AppText></>}
    <Controller control={control} name="status" render={({ field }) => <View style={{ gap: 8 }}>{(['Active', 'Trial'] as const).map(value => <AppButton key={value} title={(value === 'Active' ? 'Aktif abonelik' : 'Ücretsiz deneme') + (field.value === value ? ' ✓' : '')} variant={field.value === value ? 'primary' : 'secondary'} disabled={disabled || original?.status === 'Active' && value === 'Trial'} onPress={() => { field.onChange(value); if (value === 'Trial' && !values.trialStartDate) setValue('trialStartDate', values.startDate || today); }} />)}</View>} />
    {(status === 'Trial' || values.trialStartDate || values.trialEndDate) && <>{date('trialStartDate', 'Deneme başlangıcı', values.startDate)}{date('trialEndDate', 'Deneme bitişi', values.trialStartDate || values.startDate)}{status === 'Active' && <AppButton title="Deneme tarihlerini kaldır" variant="ghost" disabled={disabled} onPress={() => { setValue('trialStartDate', ''); setValue('trialEndDate', ''); }} />}{toggle('enableTrialReminders', 'Deneme hatırlatmalarını etkinleştir')}<AppText variant="caption" tone="muted">Hatırlatma tercihi kaydedilir; bildirim gönderildiği veya telefona ulaştığı anlamına gelmez. Kullanılabilir kuralları üyelik limiti ve sunucu belirler.</AppText></>}
    {toggle('autoRenew', 'Otomatik yenileme takibi')}{date('accessEndsOn', 'Erişim bitişi · isteğe bağlı', values.startDate)}<AppButton title="Erişim bitişi belirtilmedi" variant="ghost" disabled={disabled || !values.accessEndsOn} onPress={() => setValue('accessEndsOn', '')} />
    {input('description', 'Açıklama · isteğe bağlı', 2000)}
    {!!failure && <MutationFailure error={failure} blocked={blocked} reload={() => { void reload(); }} />}
    <AppButton title={original ? 'Abonelik değişikliklerini kaydet' : 'Aboneliği oluştur'} onPress={submit} loading={mutation.isPending} disabled={disabled || trackingLimit || paymentLimit} />
    <AppText variant="caption" tone="muted">Takip kaydı ve bağlı tek ödeme planı birlikte oluşturulur. Dış sağlayıcıdan satın alma veya tahsilat yapılmaz.</AppText>
  </View></AppScreen>;
}
