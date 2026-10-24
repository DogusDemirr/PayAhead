import { useRef, useState } from 'react';
import { Alert, Switch, View } from 'react-native';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { isCancel } from 'axios';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, AppInput, LoadingSkeleton, EmptyState } from '../../components/ui';
import { FinanceError } from '../payments/components';
import { usePayment, useEntitlements } from '../payments/queries';
import { useReminders, useNotificationSettings, useNotificationMutation } from './queries';
import { reminderFormSchema, type ReminderFields, type Reminder } from './contracts';
import { TimeField } from './TimeField';
import { ApiError } from '../../services/api-error';
export default function RemindersScreen({ plan }: { plan: string }) {
  const payment = usePayment(plan); const list = useReminders(plan); const settings = useNotificationSettings(); const entitlement = useEntitlements();
  const [editing, setEditing] = useState<Reminder | null | undefined>(undefined); const [revision, setRevision] = useState(0);
  const mutation = useNotificationMutation(); const busy = useRef(false); const [failure, setFailure] = useState<unknown>(null);
  const reload = async () => { const result = await list.refetch(); if (result.isSuccess) { setEditing(undefined); setFailure(null); setRevision(n => n + 1); } };
  const remove = async (id: string) => { if (busy.current) return; busy.current = true; setFailure(null); try { await mutation.mutateAsync({ kind: 'deleteReminder', plan, id }); } catch (e) { if (!isCancel(e)) setFailure(e); } finally { busy.current = false; } };
  return <AppScreen refreshing={list.isRefetching} onRefresh={() => { void reload(); void settings.refetch(); }}><AppHeader title="Ödeme hatırlatmaları" subtitle={payment.data?.name} back />
    {payment.isError || list.isError || settings.isError ? <FinanceError error={payment.error ?? list.error ?? settings.error} retry={() => { void payment.refetch(); void reload(); void settings.refetch(); }} /> : !payment.data || !list.data || !settings.data ? <LoadingSkeleton /> : <View style={{ gap: 16 }}>
      <AppCard><AppText variant="section">Hesap saat dilimi: {settings.data.timeZoneId}</AppText><AppText tone="muted" style={{ marginTop: 10 }}>Saatler bu hesap saat dilimindedir. Sunucu yaz saati boşluğunda ilk geçerli dakikayı, tekrarlanan saatte geç UTC anını seçer. Takvim ve UTC zamanlaması sunucudadır.</AppText>{settings.data.usedTimeZoneFallback && <AppText tone="warning">Saat dilimi geçersiz; sunucu UTC yedeği kullanıyor.</AppText>}{!settings.data.notificationsEnabled && <AppText tone="warning">Hesap bildirimleri kapalı; kural kaydetmek gönderimi açmaz.</AppText>}{!payment.data.isActive && <AppText tone="warning">Plan pasif; yeni ödeme bildirimleri gönderilmez.</AppText>}<AppButton title="Bildirim izinleri ve ayarları" variant="secondary" onPress={() => router.push('/notifications')} /></AppCard>
      {list.data.length ? list.data.map(rule => <AppCard key={rule.id}><AppText variant="section">{rule.daysBefore === 0 ? 'Ödeme günü' : `${rule.daysBefore} gün önce`} · {rule.timeOfDay}</AppText><AppText tone="muted">{rule.isEnabled ? 'Kural etkin' : 'Kural kapalı'} · Teslim kanıtı değildir</AppText><View style={{ gap: 8, marginTop: 12 }}><AppButton title={`${rule.daysBefore} gün ${rule.timeOfDay} düzenle`} variant="secondary" disabled={mutation.isPending || !!failure} onPress={() => { setEditing(rule); setRevision(n => n + 1); }} /><AppButton title={`${rule.daysBefore} gün ${rule.timeOfDay} sil`} variant="ghost" disabled={mutation.isPending || !!failure} onPress={() => Alert.alert('Hatırlatma silinsin mi?', 'Bu kuralın henüz gönderilmemiş işleri iptal edilir.', [{ text: 'Vazgeç', style: 'cancel' }, { text: 'Sil', style: 'destructive', onPress: () => { void remove(rule.id); } }])} /></View></AppCard>) : <EmptyState title="Henüz ödeme hatırlatması yok" message="Ödeme günü veya öncesi için bir kural oluştur." />}
      {failure ? <FinanceError error={failure} retry={() => { void reload(); }} /> : <AppButton title="Hatırlatma ekle" disabled={list.data.length >= 10} onPress={() => { setEditing(null); setRevision(n => n + 1); }} />}
      {editing !== undefined && <ReminderForm key={revision} plan={plan} rule={editing ?? undefined} activeCount={list.data.filter(r => r.isEnabled).length} limit={entitlement.data?.limits.activeRemindersPerPlan} done={() => setEditing(undefined)} reload={reload} />}
      <AppText tone="muted" variant="caption">Ücretsiz deneme kuralları bu listede değildir; abonelik detayındaki deneme tercihiyle yönetilir. Üyelik kotası sunucuda tüm etkin kurallara uygulanır. Geçmiş işler için sunucunun 24 saatlik telafi penceresi vardır; mobil bağımsız alarm kurmaz.</AppText>
    </View>}
  </AppScreen>;
}
export function ReminderForm({ plan, rule, activeCount, limit, done, reload }: { plan: string; rule?: Reminder; activeCount: number; limit?: number; done(): void; reload(): Promise<void> }) {
  const mutation = useNotificationMutation(); const busy = useRef(false); const [blocked, setBlocked] = useState(false); const [failure, setFailure] = useState<unknown>(null);
  const { control, handleSubmit, setValue, formState: { errors } } = useForm<ReminderFields>({ resolver: zodResolver(reminderFormSchema), defaultValues: { daysBefore: String(rule?.daysBefore ?? 1), timeOfDay: rule?.timeOfDay ?? '09:00', isEnabled: rule?.isEnabled ?? true } });
  const values = useWatch({ control }); const days = values.daysBefore; const limited = limit !== undefined && !!values.isEnabled && !rule?.isEnabled && activeCount >= limit;
  const submit = () => handleSubmit(async values => { if (busy.current || blocked || limited) return; busy.current = true; setFailure(null); try { await mutation.mutateAsync({ kind: 'saveReminder', plan, id: rule?.id, body: { daysBefore: Number(values.daysBefore), timeOfDay: values.timeOfDay, isEnabled: values.isEnabled } }); done(); } catch (e) { if (!isCancel(e)) { setFailure(e); if (e instanceof ApiError && (e.status === 409 || e.kind === 'timeout' || e.kind === 'network')) setBlocked(true); } } finally { busy.current = false; } })();
  const disabled = mutation.isPending || blocked;
  return <AppCard><View style={{ gap: 16 }}><AppText variant="section">{rule ? 'Hatırlatmayı düzenle' : 'Yeni hatırlatma'}</AppText><View style={{ gap: 8 }}>{[0, 1, 3, 7].map(value => <AppButton key={value} title={(value === 0 ? 'Ödeme günü' : `${value} gün önce`) + (days === String(value) ? ' ✓' : '')} variant={days === String(value) ? 'primary' : 'secondary'} disabled={disabled} onPress={() => setValue('daysBefore', String(value))} />)}</View>
    <Controller control={control} name="daysBefore" render={({ field }) => <AppInput label="Kaç gün önce · 0–365" value={field.value} onChangeText={field.onChange} keyboardType="number-pad" maxLength={3} editable={!disabled} error={errors.daysBefore?.message} />} />
    <Controller control={control} name="timeOfDay" render={({ field }) => <TimeField value={field.value} onChange={field.onChange} disabled={disabled} error={errors.timeOfDay?.message} />} />
    <Controller control={control} name="isEnabled" render={({ field }) => <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}><AppText>Kural etkin</AppText><Switch accessibilityLabel="Hatırlatma kuralı etkin" value={field.value} onValueChange={field.onChange} disabled={disabled} /></View>} />
    {(limited || failure instanceof ApiError && failure.code === 'entitlement_limit') && <><AppText tone="warning">Etkin hatırlatma sınırına ulaştın. Free/Premium sınırını sunucu belirler; deneme kuralları da kotayı etkileyebilir.</AppText><AppButton title="Üyelik seçeneklerini incele" variant="secondary" onPress={() => router.push('/premium')} /></>}
    {!!failure && <AppText accessibilityRole="alert" tone="danger">{failure instanceof ApiError ? failure.message : 'İşlem tamamlanamadı.'}</AppText>}{blocked && <AppButton title="Güncel hatırlatmaları yükle" variant="secondary" onPress={() => { void reload(); }} />}
    <AppButton title={rule ? 'Hatırlatma değişikliklerini kaydet' : 'Hatırlatmayı kaydet'} onPress={() => { void submit(); }} loading={mutation.isPending} disabled={disabled || limited} /><AppButton title="Formu kapat" variant="ghost" disabled={mutation.isPending} onPress={done} />
    <AppText variant="caption" tone="muted">Kural kaydı, izin veya Expo ticket kabulü telefona teslim edildiği anlamına gelmez. Saat dilimini bildirim ayarlarından kontrol et.</AppText>
  </View></AppCard>;
}
