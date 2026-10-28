import { useAppDialog } from '../../components/dialogs';
import { currentLocale, t, useLocaleRevision } from '../../i18n/runtime';
import { useRef, useState } from 'react';
import { View } from 'react-native';
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
export default function SubscriptionDetailScreen({ id }: {
    id: string;
}) {
    useLocaleRevision();
    const query = useSubscription(id);
    const [revision, setRevision] = useState(0);
    const reload = async () => {
        const result = await query.refetch();
        if (result.isSuccess)
            setRevision(n => n + 1);
    };
    if (query.isError)
        return <AppScreen><AppHeader title={t("subscriptions:m_5a3414a08005")} back/><FinanceError error={query.error} retry={() => { void reload(); }}/></AppScreen>;
    if (!query.data)
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    return <SubscriptionDetail key={revision} initial={query.data} reload={reload}/>;
}
export function SubscriptionDetail({ initial, reload }: {
    initial: Subscription;
    reload: () => Promise<void>;
}) {
    const dialogs = useAppDialog();
    useLocaleRevision();
    const [record, setRecord] = useState(initial);
    const [accessEnd, setAccessEnd] = useState(initial.accessEndsOn && initial.accessEndsOn >= initial.context.today ? initial.accessEndsOn : '');
    const mutation = useSubscriptionMutation();
    const busy = useRef(false);
    const [failure, setFailure] = useState<unknown>(null);
    const [blocked, setBlocked] = useState(false);
    const [success, setSuccess] = useState(false);
    const mutable = ['Active', 'Trial'].includes(record.status);
    const minimum = record.startDate > record.context.today ? record.startDate : record.context.today;
    async function change(kind: 'cancel' | 'delete') {
        if (busy.current || blocked)
            return;
        busy.current = true;
        setFailure(null);
        setSuccess(false);
        try {
            const result = await mutation.mutateAsync(kind === 'cancel' ? { kind, id: record.id, rowVersion: record.rowVersion, accessEndsOn: accessEnd } : { kind, id: record.id, rowVersion: record.rowVersion });
            if (kind === 'delete')
                router.dismissTo('/(tabs)/subscriptions');
            else if (result) {
                setRecord(result);
                setSuccess(true);
            }
        }
        catch (error) {
            if (!isCancel(error)) {
                setFailure(error);
                if (error instanceof ApiError && (error.status === 409 || error.kind === 'network' || error.kind === 'timeout'))
                    setBlocked(true);
            }
            throw error;
        }
        finally {
            busy.current = false;
        }
    }
    const date = (value: string | null) => value ? dateLabel(value, true, true) : t("subscriptions:m_8fd6cbabf5c7");
    const hint = trialHint(record);
    return <AppScreen refreshing={false} onRefresh={() => { void reload(); }}><AppHeader title={t("subscriptions:m_5a3414a08005")} subtitle={record.providerName} back/><View style={{ gap: 18 }}>
    <AppCard><StatusBadge label={statusLabels[record.status]} tone={record.status === 'Trial' ? 'warning' : record.status === 'Active' ? 'success' : 'muted'}/><AppText variant="title" style={{ marginTop: 16 }}>{record.name}</AppText><View style={{ marginVertical: 18 }}><FinancialAmount amount={record.amount} currency={record.currencyCode} large/></View>
      <Detail label={t("subscriptions:m_cdcb6a19c6d2")} value={t("payments:m_82413ba35131", { value0: frequencyLabels[record.billingFrequency], value1: record.billingInterval })}/><Detail label={t("subscriptions:m_39b03995fcce")} value={date(record.startDate)}/><Detail label={t("subscriptions:m_c1c27516789b")} value={date(record.firstBillingDate)}/><Detail label={t("subscriptions:m_8c74b392a2f7")} value={date(record.nextBillingDate)}/><Detail label={t("subscriptions:m_38432ff279cf")} value={date(record.trialStartDate)}/><Detail label={t("subscriptions:m_ca1dff322c77")} value={date(record.trialEndDate)}/>
      {hint && <AppText tone="warning" style={{ marginTop: 14 }}>{hint}</AppText>}<Detail label={t("subscriptions:m_c9f213296e2e")} value={record.autoRenew ? t("subscriptions:m_bbb1132f30f1") : t("subscriptions:m_39ce8f387818")}/><Detail label={t("subscriptions:m_ad24d7cfe429")} value={date(record.accessEndsOn)}/>
      <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{accessHint(record)}</AppText>{record.cancelledAtUtc && <Detail label={t("subscriptions:m_0a95b6578caa")} value={new Date(record.cancelledAtUtc).toLocaleString(currentLocale())}/>}
      <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t("subscriptions:m_5e19d63b6120", { value0: date(record.context.today), value1: record.context.timeZoneId, value2: record.context.usedTimeZoneFallback ? t("subscriptions:m_623fc8183b37") : '' })}</AppText>
      {record.description && <AppText style={{ marginTop: 16 }}>{record.description}</AppText>}
    </AppCard>
    <AppButton title={t("subscriptions:m_e6ecb5aacb90")} variant="secondary" onPress={() => router.push({ pathname: '/payment', params: { id: record.paymentPlanId, readOnly: 'true' } })}/>
    <CatalogIdentity categoryId={record.catalogCategoryId} serviceId={record.catalogServiceId} fallback={record.category ?? t("subscriptions:m_cd0d38999b7e")}/>
    <AppButton title={t("subscriptions:m_a28ea32a92a3")} variant="secondary" onPress={() => router.push({ pathname: '/reminders', params: { id: record.paymentPlanId } })}/>
    <AppCard><AppText variant="label">{t("subscriptions:m_d0ae165f2ea1", { value0: record.enableTrialReminders ? t("subscriptions:m_a139ddfe6c16") : t("subscriptions:m_d38040e92e84") })}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>{t("subscriptions:m_120955beb4d9")}</AppText></AppCard>
    <AppText variant="caption" tone="muted">{t("subscriptions:m_af18c85b8d43")}</AppText>
    {mutable ? <><AppButton title={t("subscriptions:m_8810c95e97ad")} disabled={mutation.isPending || blocked} onPress={() => router.push({ pathname: '/edit-subscription', params: { id: record.id } })}/>
      <AppCard><AppText variant="section">{t("subscriptions:m_9c855e9dea46")}</AppText><AppText tone="muted" style={{ marginVertical: 14 }}>{t("subscriptions:m_8c19506de966")}</AppText><DateField label={t("subscriptions:m_09ac97477bbd")} value={accessEnd} onChange={setAccessEnd} fallback={minimum} minimum={minimum} disabled={mutation.isPending || blocked}/>
        <View style={{ marginTop: 16 }}><AppButton title={t("subscriptions:m_2487cd5b4552")} variant="secondary" disabled={mutation.isPending || blocked || !validDate(accessEnd) || accessEnd < minimum} onPress={() => dialogs.alert(t("common:m_74fddbd51925"), t("subscriptions:m_307f11592d33", { value0: dateLabel(accessEnd, true, true) }), [{ text: t("subscriptions:m_39a8d28c1c6b"), style: 'cancel' }, { text: t("subscriptions:m_2c5a5df10ac9"), onPress: () => change('cancel') }])}/></View></AppCard></> : <AppCard><AppText tone="muted">{t("subscriptions:m_dc2fe2ce17ea")}</AppText></AppCard>}
    <AppButton title={t("subscriptions:m_a73ff22c9a90")} variant="ghost" disabled={mutation.isPending || blocked} onPress={() => dialogs.alert(t("common:m_5c0481ebe4d9"), t("common:m_d3579181350f"), [{ text: t("subscriptions:m_39a8d28c1c6b"), style: 'cancel' }, { text: t("subscriptions:m_b88019aa289e"), style: 'destructive', onPress: () => change('delete') }])}/>
    {mutation.isPending && <AppText accessibilityLiveRegion="polite">{t("subscriptions:m_5230f2b7aaa9")}</AppText>}{success && <AppText accessibilityRole="alert" tone="success">{t("subscriptions:m_e8acb1ee980d")}</AppText>}
    {failure instanceof ApiError && failure.code === 'entitlement_limit' && <SubscriptionLimitNotice />}{!!failure && <MutationFailure error={failure} blocked={blocked} reload={() => { void reload(); }}/>}
  </View>{dialogs.dialog}</AppScreen>;
}
function Detail({ label, value }: {
    label: string;
    value: string;
}) { useLocaleRevision(); return <View style={{ gap: 4, marginTop: 12 }}><AppText tone="muted" variant="caption">{label}</AppText><AppText variant="label">{value}</AppText></View>; }
