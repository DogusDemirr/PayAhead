import { t, useLocaleRevision } from "../../i18n/runtime";
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
export default function RemindersScreen({ plan }: {
    plan: string;
}) {
    useLocaleRevision();
    const payment = usePayment(plan);
    const list = useReminders(plan);
    const settings = useNotificationSettings();
    const entitlement = useEntitlements();
    const [editing, setEditing] = useState<Reminder | null | undefined>(undefined);
    const [revision, setRevision] = useState(0);
    const mutation = useNotificationMutation();
    const busy = useRef(false);
    const [failure, setFailure] = useState<unknown>(null);
    const reload = async () => {
        const result = await list.refetch();
        if (result.isSuccess) {
            setEditing(undefined);
            setFailure(null);
            setRevision(n => n + 1);
        }
    };
    const remove = async (id: string) => {
        if (busy.current)
            return;
        busy.current = true;
        setFailure(null);
        try {
            await mutation.mutateAsync({ kind: 'deleteReminder', plan, id });
        }
        catch (e) {
            if (!isCancel(e))
                setFailure(e);
        }
        finally {
            busy.current = false;
        }
    };
    return <AppScreen refreshing={list.isRefetching} onRefresh={() => { void reload(); void settings.refetch(); }}><AppHeader title={t("reminders:m_ea6119aa5393")} subtitle={payment.data?.name} back/>
    {payment.isError || list.isError || settings.isError ? <FinanceError error={payment.error ?? list.error ?? settings.error} retry={() => { void payment.refetch(); void reload(); void settings.refetch(); }}/> : !payment.data || !list.data || !settings.data ? <LoadingSkeleton /> : <View style={{ gap: 16 }}>
      <AppCard><AppText variant="section">{t("reminders:m_3420bbb71219", { value0: settings.data.timeZoneId })}</AppText><AppText tone="muted" style={{ marginTop: 10 }}>{t("reminders:m_68dce5fdd9e4")}</AppText>{settings.data.usedTimeZoneFallback && <AppText tone="warning">{t("reminders:m_3771184e8e48")}</AppText>}{!settings.data.notificationsEnabled && <AppText tone="warning">{t("reminders:m_66d73eec21dd")}</AppText>}{!payment.data.isActive && <AppText tone="warning">{t("reminders:m_8d54e58502d4")}</AppText>}<AppButton title={t("reminders:m_424dbf42bf81")} variant="secondary" onPress={() => router.push('/notifications')}/></AppCard>
      {list.data.length ? list.data.map(rule => <AppCard key={rule.id}><AppText variant="section">{rule.daysBefore === 0 ? t("reminders:m_ff4947e183f7") : t("reminders:m_32cab8a0ff96", { value0: rule.daysBefore })} · {rule.timeOfDay}</AppText><AppText tone="muted">{t("reminders:m_d50fb5cd0e44", { value0: rule.isEnabled ? t("reminders:m_82bd83d6cabb") : t("reminders:m_788c248434f3") })}</AppText><View style={{ gap: 8, marginTop: 12 }}><AppButton title={t("reminders:m_86471fc0130e", { value0: rule.daysBefore, value1: rule.timeOfDay })} variant="secondary" disabled={mutation.isPending || !!failure} onPress={() => { setEditing(rule); setRevision(n => n + 1); }}/><AppButton title={t("reminders:m_7d21de34f624", { value0: rule.daysBefore, value1: rule.timeOfDay })} variant="ghost" disabled={mutation.isPending || !!failure} onPress={() => Alert.alert(t("common:m_dc2b092891f8"), t("common:m_635302bd313c"), [{ text: t("reminders:m_39a8d28c1c6b"), style: 'cancel' }, { text: t("common:m_f698e9b40f9f"), style: 'destructive', onPress: () => { void remove(rule.id); } }])}/></View></AppCard>) : <EmptyState title={t("reminders:m_14842beb581f")} message={t("reminders:m_44331cdb99ad")}/>}
      {failure ? <FinanceError error={failure} retry={() => { void reload(); }}/> : <AppButton title={t("reminders:m_439e3be67e95")} disabled={list.data.length >= 10} onPress={() => { setEditing(null); setRevision(n => n + 1); }}/>}
      {editing !== undefined && <ReminderForm key={revision} plan={plan} rule={editing ?? undefined} activeCount={list.data.filter(r => r.isEnabled).length} limit={entitlement.data?.limits.activeRemindersPerPlan} done={() => setEditing(undefined)} reload={reload}/>}
      <AppText tone="muted" variant="caption">{t("reminders:m_77b658b9666c")}</AppText>
    </View>}
  </AppScreen>;
}
export function ReminderForm({ plan, rule, activeCount, limit, done, reload }: {
    plan: string;
    rule?: Reminder;
    activeCount: number;
    limit?: number;
    done(): void;
    reload(): Promise<void>;
}) {
    useLocaleRevision();
    const mutation = useNotificationMutation();
    const busy = useRef(false);
    const [blocked, setBlocked] = useState(false);
    const [failure, setFailure] = useState<unknown>(null);
    const { control, handleSubmit, setValue, formState: { errors } } = useForm<ReminderFields>({ resolver: zodResolver(reminderFormSchema), defaultValues: { daysBefore: String(rule?.daysBefore ?? 1), timeOfDay: rule?.timeOfDay ?? '09:00', isEnabled: rule?.isEnabled ?? true } });
    const values = useWatch({ control });
    const days = values.daysBefore;
    const limited = limit !== undefined && !!values.isEnabled && !rule?.isEnabled && activeCount >= limit;
    const submit = () => handleSubmit(async (values) => {
        if (busy.current || blocked || limited)
            return;
        busy.current = true;
        setFailure(null);
        try {
            await mutation.mutateAsync({ kind: 'saveReminder', plan, id: rule?.id, body: { daysBefore: Number(values.daysBefore), timeOfDay: values.timeOfDay, isEnabled: values.isEnabled } });
            done();
        }
        catch (e) {
            if (!isCancel(e)) {
                setFailure(e);
                if (e instanceof ApiError && (e.status === 409 || e.kind === 'timeout' || e.kind === 'network'))
                    setBlocked(true);
            }
        }
        finally {
            busy.current = false;
        }
    })();
    const disabled = mutation.isPending || blocked;
    return <AppCard><View style={{ gap: 16 }}><AppText variant="section">{rule ? t("reminders:m_46954ab8fe15") : t("reminders:m_5bfd5c4fc74c")}</AppText><View style={{ gap: 8 }}>{[0, 1, 3, 7].map(value => <AppButton key={value} title={(value === 0 ? t("reminders:m_ff4947e183f7") : t("reminders:m_32cab8a0ff96", { value0: value })) + (days === String(value) ? ' ✓' : '')} variant={days === String(value) ? 'primary' : 'secondary'} disabled={disabled} onPress={() => setValue('daysBefore', String(value))}/>)}</View>
    <Controller control={control} name="daysBefore" render={({ field }) => <AppInput label={t("reminders:m_e67c2a090f36")} value={field.value} onChangeText={field.onChange} keyboardType="number-pad" maxLength={3} editable={!disabled} error={errors.daysBefore?.message}/>}/>
    <Controller control={control} name="timeOfDay" render={({ field }) => <TimeField value={field.value} onChange={field.onChange} disabled={disabled} error={errors.timeOfDay?.message}/>}/>
    <Controller control={control} name="isEnabled" render={({ field }) => <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}><AppText>{t("reminders:m_82bd83d6cabb")}</AppText><Switch accessibilityLabel={t("reminders:m_70ba032f779d")} value={field.value} onValueChange={field.onChange} disabled={disabled}/></View>}/>
    {(limited || failure instanceof ApiError && failure.code === 'entitlement_limit') && <><AppText tone="warning">{t("reminders:m_be3095cc7a25")}</AppText><AppButton title={t("reminders:m_5cc017022589")} variant="secondary" onPress={() => router.push('/premium')}/></>}
    {!!failure && <AppText accessibilityRole="alert" tone="danger">{failure instanceof ApiError ? failure.message : t("reminders:m_643f2c5b4ff3")}</AppText>}{blocked && <AppButton title={t("reminders:m_73922fe9b8e9")} variant="secondary" onPress={() => { void reload(); }}/>}
    <AppButton title={rule ? t("reminders:m_a5ef4ea66a75") : t("reminders:m_0406a6037ff3")} onPress={() => { void submit(); }} loading={mutation.isPending} disabled={disabled || limited}/><AppButton title={t("reminders:m_17d89dfee53d")} variant="ghost" disabled={mutation.isPending} onPress={done}/>
    <AppText variant="caption" tone="muted">{t("reminders:m_ffadd8e17995")}</AppText>
  </View></AppCard>;
}
