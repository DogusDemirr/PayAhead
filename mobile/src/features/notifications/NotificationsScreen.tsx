import { currentLanguage, currentLocale, t, useLocaleRevision } from '../../i18n/runtime';
import { useEffect, useRef, useState } from 'react';
import { Alert, Linking, View, Switch } from 'react-native';
import { Controller, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { isCancel } from 'axios';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, AppInput, LoadingSkeleton, SectionHeader, EmptyState } from '../../components/ui';
import { FinanceError } from '../payments/components';
import { useDevices, useNotificationSettings, useNotificationMutation } from './queries';
import { settingsFormSchema, type NotificationSettings, type SettingsWrite } from './contracts';
import { usePushState, pushAvailability, connectPush, disconnectPush, refreshPushPermission, acknowledgeRemovedDevice } from './push-runtime';
import { ApiError } from '../../services/api-error';
export default function NotificationsScreen() {
    useLocaleRevision();
    useEffect(() => { void refreshPushPermission().catch(() => usePushState.setState({ error: t("reminders:m_7900e641cb36") })); }, []);
    const settings = useNotificationSettings();
    const devices = useDevices();
    const push = usePushState();
    const mutation = useNotificationMutation();
    const busy = useRef(false);
    const [failure, setFailure] = useState<unknown>(null);
    const [revision, setRevision] = useState(0);
    const reload = async () => { const result = await settings.refetch(); await devices.refetch(); try {
        await refreshPushPermission();
    }
    catch {
        usePushState.setState({ error: t("reminders:m_7900e641cb36") });
    } if (result.isSuccess) {
        setFailure(null);
        setRevision(n => n + 1);
    } };
    const remove = async (id: string) => { if (busy.current)
        return; busy.current = true; try {
        await mutation.mutateAsync({ kind: 'deleteDevice', id });
        await acknowledgeRemovedDevice(id);
    }
    catch (e) {
        if (!isCancel(e))
            setFailure(e);
    }
    finally {
        busy.current = false;
    } };
    return <AppScreen refreshing={settings.isRefetching || devices.isRefetching} onRefresh={() => { void reload(); }}><AppHeader title={t("reminders:m_996149c57168")} subtitle={t("reminders:m_5c778761062b")} back/><View style={{ gap: 18 }}>
    <AppCard><AppText variant="section">{t("reminders:m_796e7d39b546")}</AppText><AppText style={{ marginVertical: 12 }}>{push.permission === 'granted' ? t("reminders:m_49e62454340a") : push.permission === 'denied' ? t("reminders:m_9376cf52ba0d") : t("reminders:m_95ed0d227581")}</AppText><AppText tone="muted">{t("reminders:m_e41167a535f4")}</AppText>
      {pushAvailability() && <AppText tone="warning" style={{ marginVertical: 12 }}>{pushAvailability()}</AppText>}<AppText variant="caption" style={{ marginVertical: 12 }}>{push.registered ? t("reminders:m_1679d0879814") : t("reminders:m_6122961c79fc")}</AppText>
      <View style={{ gap: 10 }}><AppButton title={t("reminders:m_26b8a5c03dfb")} disabled={!!pushAvailability() || push.busy} loading={push.busy} onPress={() => { void connectPush(true); }}/><AppButton title={t("reminders:m_aba251b45799")} variant="secondary" onPress={() => { void Linking.openSettings().catch(() => usePushState.setState({ error: t("reminders:m_e1670ba6707d") })); }}/><AppButton title={t("reminders:m_4a3cd0d3f2ee")} variant="ghost" disabled={push.busy} onPress={() => { void disconnectPush().catch(() => usePushState.setState({ error: t("reminders:m_a9a9861c603e") })); }}/></View>
      {!push.canAskAgain && <AppText tone="warning">{t("reminders:m_082675ccc2b8")}</AppText>}{push.error && <AppText tone="danger" accessibilityRole="alert" style={{ marginTop: 12 }}>{push.error}</AppText>}
    </AppCard>
    {settings.isError ? <FinanceError error={settings.error} retry={() => { void reload(); }}/> : !settings.data ? <LoadingSkeleton /> : <SettingsForm key={revision} initial={settings.data} reload={reload}/>}
    <SectionHeader title={t("reminders:m_55352df66012")}/>{devices.isError ? <FinanceError error={devices.error} retry={() => { void devices.refetch(); }}/> : !devices.data ? <LoadingSkeleton /> : devices.data.length ? devices.data.map((d, index) => <AppCard key={d.id}><AppText variant="label">{t("reminders:m_aa3cf0d408e7", { value0: d.platform === 'android' ? 'Android' : 'iOS', value1: index + 1 })}</AppText><AppText tone="muted">{t("reminders:m_dd339f27d49c", { value0: d.isActive ? t("reminders:m_b44948b30289") : t("reminders:m_dc1ec4a10b02"), value1: new Date(d.lastSeenAtUtc).toLocaleString(currentLocale()) })}</AppText><AppButton title={t("reminders:m_14477af7fb8f", { value0: index + 1 })} variant="ghost" disabled={mutation.isPending || !!failure} onPress={() => Alert.alert(t("common:m_32818902a3d4"), t("common:m_3608ae3ad221"), [{ text: t("reminders:m_39a8d28c1c6b"), style: 'cancel' }, { text: t("reminders:m_b88019aa289e"), style: 'destructive', onPress: () => { void remove(d.id); } }])}/></AppCard>) : <EmptyState title={t("reminders:m_e9e3db3d1e8f")} message={t("reminders:m_cc44fa0414a3")}/>}
    {!!failure && <FinanceError error={failure} retry={() => { setFailure(null); void reload(); }}/>}<AppText variant="caption" tone="muted">{t("reminders:m_8a2d59b0f70d")}</AppText>
  </View></AppScreen>;
}
export function SettingsForm({ initial, reload }: {
    initial: NotificationSettings;
    reload(): Promise<void>;
}) {
    useLocaleRevision();
    const mutation = useNotificationMutation();
    const busy = useRef(false);
    const [failure, setFailure] = useState<unknown>(null);
    const [blocked, setBlocked] = useState(false);
    const [saved, setSaved] = useState(false);
    const { control, handleSubmit, formState: { errors } } = useForm<SettingsWrite>({ resolver: zodResolver(settingsFormSchema), defaultValues: { languageCode: initial.languageCode, defaultCurrencyCode: initial.defaultCurrencyCode, timeZoneId: initial.timeZoneId, notificationsEnabled: initial.notificationsEnabled } });
    const disabled = mutation.isPending || blocked;
    const submit = () => handleSubmit(async (body) => { if (busy.current || blocked)
        return; busy.current = true; setSaved(false); setFailure(null); try {
        await mutation.mutateAsync({ kind: 'settings', body: { ...body, languageCode: currentLanguage() } });
        setSaved(true);
    }
    catch (e) {
        if (!isCancel(e)) {
            setFailure(e);
            if (e instanceof ApiError && (e.status === 409 || e.kind === 'network' || e.kind === 'timeout'))
                setBlocked(true);
        }
    }
    finally {
        busy.current = false;
    } })();
    return <AppCard><View style={{ gap: 16 }}><AppText variant="section">{t("reminders:m_decce11aef29")}</AppText>{initial.usedTimeZoneFallback && <AppText tone="warning">{t("reminders:m_bc3703640ed8")}</AppText>}
    <Controller control={control} name="notificationsEnabled" render={({ field }) => <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}><AppText style={{ flex: 1 }}>{t("reminders:m_99fca4be5948")}</AppText><Switch accessibilityLabel={t("reminders:m_99fca4be5948")} value={field.value} onValueChange={field.onChange} disabled={disabled}/></View>}/>
    <Controller control={control} name="timeZoneId" render={({ field }) => <AppInput label={t("reminders:m_ca7732645140")} placeholder={t("reminders:m_29085b405b36")} value={field.value} onChangeText={field.onChange} autoCapitalize="none" editable={!disabled} error={errors.timeZoneId?.message} maxLength={100}/>}/>
    <Controller control={control} name="defaultCurrencyCode" render={({ field }) => <AppInput label={t("reminders:m_c958a8d64dde")} value={field.value} onChangeText={field.onChange} editable={!disabled} autoCapitalize="characters" maxLength={3} error={errors.defaultCurrencyCode?.message}/>}/>
    <AppText variant="caption" tone="muted">{t("reminders:m_dcafb2602a42")}</AppText>
    {!!failure && <AppText tone="danger" accessibilityRole="alert">{failure instanceof ApiError ? failure.message : t("reminders:m_ebf40e0c6eb4")}</AppText>}{blocked && <AppButton title={t("reminders:m_25353ac12585")} variant="secondary" onPress={() => { void reload(); }}/>}<AppButton title={t("reminders:m_78a55771dc4b")} disabled={disabled} loading={mutation.isPending} onPress={() => { void submit(); }}/>{saved && <AppText tone="success" accessibilityRole="alert">{t("reminders:m_bf1fb9b3ea59")}</AppText>}
  </View></AppCard>;
}
