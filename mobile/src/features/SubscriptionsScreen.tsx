import { t, useLocaleRevision } from "../i18n/runtime";
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { Plus } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppInput, AppButton, IconButton, SectionHeader, EmptyState, LoadingSkeleton } from '../components/ui';
import { FinanceError } from './payments/components';
import { useEntitlements } from './payments/queries';
import { currencyCodes } from './payments/currencies';
import { useSubscriptions, useSubscriptionSummary } from './subscriptions/queries';
import { SubscriptionRow, SubscriptionTotals, statusLabels } from './subscriptions/components';
import type { SubscriptionStatus } from './subscriptions/contracts';
import { dateLabel } from '../utils/format';
export default function SubscriptionsScreen() {
    useLocaleRevision();
    const [search, setSearch] = useState('');
    const [debounced, setDebounced] = useState('');
    const [page, setPage] = useState(1);
    const [status, setStatus] = useState<SubscriptionStatus>();
    const [currency, setCurrency] = useState('');
    const [appliedCurrency, setAppliedCurrency] = useState<string>();
    const [filterError, setFilterError] = useState('');
    useEffect(() => { const timer = setTimeout(() => { setDebounced(search.trim()); setPage(1); }, 300); return () => clearTimeout(timer); }, [search]);
    const list = useSubscriptions({ pageNumber: page, pageSize: 20, status, search: debounced || undefined, currencyCode: appliedCurrency });
    const summary = useSubscriptionSummary();
    const entitlement = useEntitlements();
    const refresh = () => { void list.refetch(); void summary.refetch(); void entitlement.refetch(); };
    return <AppScreen refreshing={list.isRefetching || summary.isRefetching} onRefresh={refresh}><AppHeader title={t("common:m_305a7abea88b")} subtitle={t("common:m_056559cd4769")} right={<IconButton icon={Plus} label={t("common:m_3bb51670d4e2")} onPress={() => router.push('/add-subscription')}/>}/>
    {summary.isError ? <FinanceError error={summary.error} retry={() => { void summary.refetch(); }}/> : !summary.data ? <LoadingSkeleton /> : <><SubscriptionTotals data={summary.data}/><AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t("common:m_5e19d63b6120", { value0: dateLabel(summary.data.context.today, false, true), value1: summary.data.context.timeZoneId, value2: summary.data.context.usedTimeZoneFallback ? t("common:m_1b794efacab7") : '' })}</AppText>
      {entitlement.data && <AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>{t("common:m_02222f976398", { value0: entitlement.data.plan, value1: summary.data.activeSubscriptionCount + summary.data.trialSubscriptionCount, value2: entitlement.data.limits.activeTrackedSubscriptions })}</AppText>}
      <View style={{ marginTop: 14, gap: 8 }}><AppButton title={t("common:m_b88a01eb7582", { value0: summary.data.expiringTrialsCount })} variant="secondary" onPress={() => router.push('/subscription-trials')}/><AppButton title={t("common:m_3bb51670d4e2")} onPress={() => router.push('/add-subscription')}/></View>
      {summary.data.nextRenewal && <AppCard style={{ marginTop: 16 }}><AppText tone="muted" variant="caption">{t("common:m_1e1adc209eed")}</AppText><SubscriptionRow record={summary.data.nextRenewal}/></AppCard>}</>}
    <SectionHeader title={t("common:m_fbafe9fc3173")}/><AppInput label={t("common:m_108e1b550628")} value={search} onChangeText={setSearch} maxLength={150}/>
    <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginVertical: 14 }}>{([undefined, 'Active', 'Trial', 'Cancelled', 'Expired'] as const).map(value => <View key={value ?? 'all'} style={{ minWidth: '45%', flexGrow: 1 }}><AppButton title={(value ? statusLabels[value] : t("common:m_f43a65ad8268")) + (status === value ? ' ✓' : '')} variant={status === value ? 'primary' : 'secondary'} onPress={() => { setStatus(value); setPage(1); }}/></View>)}</View>
    <View style={{ gap: 8, marginBottom: 16 }}><AppInput label={t("common:m_7060d276702b")} value={currency} onChangeText={setCurrency} maxLength={3} autoCapitalize="characters" placeholder={t("common:m_ff166f34034b")} error={filterError || undefined}/><AppButton title={t("common:m_d789bd722598")} variant="secondary" onPress={() => {
            const code = currency.trim().toUpperCase();
            if (code && !currencyCodes.includes(code)) {
                setFilterError(t("common:m_9ca92bf2de8d"));
                return;
            }
            setFilterError('');
            setAppliedCurrency(code || undefined);
            setPage(1);
        }}/></View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 10 }}>{t("common:m_4fa460adaf28")}</AppText>
    {list.isError ? <FinanceError error={list.error} retry={() => { void list.refetch(); }}/> : !list.data ? <LoadingSkeleton /> : <><AppText variant="caption" tone="muted">{t("common:m_f334350d4252", { value0: list.data.totalCount, value1: page, value2: Math.max(1, list.data.totalPages) })}</AppText>{list.data.items.length ? <AppCard>{list.data.items.map(record => <SubscriptionRow key={record.id} record={record}/>)}</AppCard> : <EmptyState title={t("common:m_2aa78fcdc0ca")} message={t("common:m_a0256836986f")} action={t("subscriptions:m_3d84aa9bcade")} onPress={() => router.push('/add-subscription')}/>}<View style={{ gap: 8, marginTop: 16 }}><AppButton title={t("common:m_028ac77e95d4")} variant="secondary" disabled={page <= 1 || list.isFetching} onPress={() => setPage(p => p - 1)}/><AppButton title={t("common:m_887c317bfdb9")} variant="secondary" disabled={page >= list.data.totalPages || list.isFetching} onPress={() => setPage(p => p + 1)}/></View></>}
  </AppScreen>;
}
