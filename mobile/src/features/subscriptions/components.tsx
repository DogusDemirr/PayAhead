import { t, useLocaleRevision } from "../../i18n/runtime";
import { View, Pressable } from 'react-native';
import { router } from 'expo-router';
import { AppCard, AppText, AppButton, StatusBadge, SectionHeader } from '../../components/ui';
import { FinanceError, FinancialAmount } from '../payments/components';
import { frequencyLabels } from '../payments/validation';
import { ApiError } from '../../services/api-error';
import { dateLabel } from '../../utils/format';
import { useSubscriptionSummary, useExpiringTrials } from './queries';
import type { Subscription, SubscriptionSummary } from './contracts';
import { CatalogIdentity } from '../catalog/CatalogIdentity';
export const statusLabels = { get Trial() {
        return t("subscriptions:m_a75abe66fec2");
    }, get Active() {
        return t("subscriptions:m_d5f884547452");
    }, get Cancelled() {
        return t("subscriptions:m_dbd417da03e1");
    }, get Expired() {
        return t("subscriptions:m_87e91c19c039");
    } } as const;
export function trialHint(record: Pick<Subscription, 'status' | 'trialStartDate' | 'trialEndDate' | 'trialDaysRemaining' | 'context'>) {
    if (!record.trialEndDate)
        return null;
    if (record.status === 'Cancelled')
        return t("subscriptions:m_eeee62ca562d");
    if (record.status === 'Expired' || record.trialEndDate < record.context.today)
        return t("subscriptions:m_4045e1f2fa19");
    if (record.trialStartDate && record.trialStartDate > record.context.today)
        return t("subscriptions:m_39ece7b68bec", { value0: dateLabel(record.trialStartDate) });
    return record.trialDaysRemaining === null ? null : record.trialDaysRemaining === 0 ? t("subscriptions:m_0499b314e350") : t("subscriptions:m_c3f52697e59c", { value0: record.trialDaysRemaining });
}
export function accessHint(record: Pick<Subscription, 'status' | 'accessEndsOn' | 'context'>) {
    if (!record.accessEndsOn)
        return t("subscriptions:m_22641ad761bf");
    if (record.status === 'Expired' || record.accessEndsOn < record.context.today)
        return t("subscriptions:m_af985fbab912", { value0: dateLabel(record.accessEndsOn, false, true) });
    return t("subscriptions:m_a2b74b09a1e6", { value0: dateLabel(record.accessEndsOn, false, true), value1: record.status === 'Cancelled' ? t("subscriptions:m_c8c32fd44bb1") : t("subscriptions:m_7b6ad902719e") });
}
export function SubscriptionRow({ record }: {
    record: Subscription;
}) {
    useLocaleRevision();
    const trial = trialHint(record);
    const urgent = record.status === 'Trial' && record.trialStartDate !== null && record.trialStartDate <= record.context.today && record.trialDaysRemaining !== null && record.trialDaysRemaining <= 7;
    return <Pressable accessibilityRole="button" accessibilityLabel={t("subscriptions:m_96a9ac11297f", { value0: record.name, value1: statusLabels[record.status] })} onPress={() => router.push({ pathname: '/subscription', params: { id: record.id } })} style={{ paddingVertical: 18, minHeight: 150, gap: 10 }}>
    <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}><View style={{ flex: 1 }}><AppText variant="section">{record.name}</AppText><AppText tone="muted" variant="caption">{t("subscriptions:m_f85cb0113896", { value0: record.providerName, value1: frequencyLabels[record.billingFrequency], value2: record.billingInterval })}</AppText></View><StatusBadge label={statusLabels[record.status]} tone={record.status === 'Trial' ? 'warning' : record.status === 'Active' ? 'success' : 'muted'}/></View>
    <FinancialAmount amount={record.amount} currency={record.currencyCode}/>
    <CatalogIdentity categoryId={record.catalogCategoryId} serviceId={record.catalogServiceId} fallback={record.category ?? t("subscriptions:m_cd0d38999b7e")}/>
    <AppText variant="caption" tone="muted">{record.nextBillingDate ? t("subscriptions:m_90e3db49d29e", { value0: dateLabel(record.nextBillingDate, false, true) }) : t("subscriptions:m_dc2c56f19435")}</AppText>
    {trial && <AppText variant="caption" tone={urgent ? 'warning' : 'muted'}>{trial}</AppText>}
    {(record.status === 'Cancelled' || record.status === 'Expired') && <AppText variant="caption" tone="muted">{accessHint(record)}</AppText>}
  </Pressable>;
}
export function SubscriptionTotals({ data }: {
    data: SubscriptionSummary;
}) {
    useLocaleRevision();
    return <AppCard><AppText variant="caption" tone="muted">{t("subscriptions:m_59bb6eb356f4")}</AppText>{data.monthlyEquivalentTotalsByCurrency.length ? data.monthlyEquivalentTotalsByCurrency.map(total => <View key={total.currencyCode} style={{ gap: 6, marginTop: 16 }}><FinancialAmount amount={total.estimatedAmount} currency={total.currencyCode} large/><AppText variant="caption" tone="muted">{t("subscriptions:m_d96738411504", { value0: total.currencyCode })}</AppText></View>) : <AppText tone="muted" style={{ marginTop: 12 }}>{t("subscriptions:m_4ecad399a85a")}</AppText>}
    <AppText style={{ marginTop: 18 }}>{t("subscriptions:m_f5fdcac75a16", { value0: data.activeSubscriptionCount, value1: data.trialSubscriptionCount, value2: data.cancelledSubscriptionCount, value3: data.expiredSubscriptionCount })}</AppText>
    <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t("subscriptions:m_1a6296fdf554")}</AppText>
  </AppCard>;
}
export function SubscriptionLimitNotice() {
    useLocaleRevision();
    return <AppCard><AppText variant="section">{t("subscriptions:m_31a31e2fdfe1")}</AppText><AppText tone="warning" style={{ marginVertical: 12 }}>{t("subscriptions:m_99e46aeb1075")}</AppText><AppButton title={t("subscriptions:m_5cc017022589")} variant="secondary" onPress={() => router.push('/premium')}/></AppCard>;
}
export function MutationFailure({ error, blocked, reload }: {
    error: unknown;
    blocked: boolean;
    reload: () => void;
}) {
    useLocaleRevision();
    return <AppCard><AppText tone="danger" accessibilityRole="alert">{error instanceof ApiError ? error.message : t("subscriptions:m_7c017ff51519")}</AppText>{blocked && <View style={{ gap: 12, marginTop: 12 }}><AppText tone="muted" variant="caption">{t("subscriptions:m_21319cec854e")}</AppText><AppButton title={t("subscriptions:m_40e816c987a2")} variant="secondary" onPress={reload}/></View>}</AppCard>;
}
export function SubscriptionDashboardCard() {
    useLocaleRevision();
    const summary = useSubscriptionSummary();
    const trials = useExpiringTrials();
    return <><SectionHeader title={t("subscriptions:m_0426bc127d33")} action={t("common:m_f43a65ad8268")} onPress={() => router.push('/(tabs)/subscriptions')}/>
    {summary.isError ? <FinanceError error={summary.error} retry={() => { void summary.refetch(); }}/> : !summary.data ? <AppCard><AppText tone="muted">{t("subscriptions:m_5a685ba46d97")}</AppText></AppCard> : <AppCard><AppText variant="section">{t("subscriptions:m_b3012876af05", { value0: summary.data.activeSubscriptionCount, value1: summary.data.trialSubscriptionCount })}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>{t("subscriptions:m_e6a9d0c56b98")}</AppText>{summary.data.nextRenewal && <CatalogIdentity categoryId={summary.data.nextRenewal.catalogCategoryId} serviceId={summary.data.nextRenewal.catalogServiceId} fallback={summary.data.nextRenewal.category}/>}{summary.data.nextRenewal && <AppText style={{ marginTop: 12 }}>{t("subscriptions:m_b8691c27fa4c", { value0: summary.data.nextRenewal.name, value1: summary.data.nextRenewal.nextBillingDate ? dateLabel(summary.data.nextRenewal.nextBillingDate) : t("subscriptions:m_37e4b6a7987d") })}</AppText>}<AppButton title={t("subscriptions:m_b88a01eb7582", { value0: summary.data.expiringTrialsCount })} variant="secondary" onPress={() => router.push('/subscription-trials')}/></AppCard>}
    {trials.isError ? <FinanceError error={trials.error} retry={() => { void trials.refetch(); }}/> : trials.data?.length ? <AppCard style={{ marginTop: 12 }}>{trials.data.slice(0, 3).map(item => <View key={item.subscriptionId} style={{ marginVertical: 8 }}><AppText variant="label">{item.name} · {item.daysRemaining === 0 ? t("subscriptions:m_b51cb4cab2c8") : t("subscriptions:m_008749f47ce5", { value0: item.daysRemaining })}</AppText><AppText variant="caption" tone="muted">{t("subscriptions:m_5a89e7b9050a", { value0: dateLabel(item.firstBillingDate, false, true) })}</AppText></View>)}</AppCard> : null}
  </>;
}
