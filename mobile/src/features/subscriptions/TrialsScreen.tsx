import { t, useLocaleRevision } from "../../i18n/runtime";
import { useState } from 'react';
import { View } from 'react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, LoadingSkeleton, EmptyState } from '../../components/ui';
import { FinancialAmount, FinanceError } from '../payments/components';
import { useExpiringTrials } from './queries';
import { dateLabel } from '../../utils/format';
export default function TrialsScreen() {
    useLocaleRevision();
    const [days, setDays] = useState(7);
    const [page, setPage] = useState(1);
    const query = useExpiringTrials(days);
    const totalPages = Math.max(1, Math.ceil((query.data?.length ?? 0) / 20));
    const currentPage = Math.min(page, totalPages);
    return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title={t("subscriptions:m_65aedc4c30ef")} subtitle={t("subscriptions:m_92aab347c863")} back/><View style={{ gap: 8, marginBottom: 20 }}>{[1, 3, 7].map(value => <AppButton key={value} title={t("subscriptions:m_1387bc38b3ee", { value0: value, value1: days === value ? ' ✓' : '' })} variant={days === value ? 'primary' : 'secondary'} onPress={() => { setDays(value); setPage(1); }}/>)}</View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 16 }}>{t("subscriptions:m_49eae7447e75")}</AppText>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }}/> : !query.data ? <LoadingSkeleton /> : query.data.length ? <>{query.data.slice((currentPage - 1) * 20, currentPage * 20).map(item => <AppCard key={item.subscriptionId} style={{ marginBottom: 14 }}><AppText variant="section">{item.name}</AppText><AppText tone="muted">{item.providerName}</AppText><AppText tone="warning" style={{ marginVertical: 12 }}>{item.daysRemaining === 0 ? t("subscriptions:m_0499b314e350") : t("subscriptions:m_008749f47ce5", { value0: item.daysRemaining })}</AppText><AppText>{t("subscriptions:m_9154c18c668d", { value0: dateLabel(item.trialEndDate, true, true) })}</AppText><AppText>{t("subscriptions:m_322ce9e70bfc", { value0: dateLabel(item.firstBillingDate, true, true) })}</AppText><View style={{ marginVertical: 12 }}><FinancialAmount amount={item.amount} currency={item.currencyCode}/></View><AppButton title={t("subscriptions:m_e88a0897c5ca", { value0: item.name })} variant="secondary" onPress={() => router.push({ pathname: '/subscription', params: { id: item.subscriptionId } })}/></AppCard>)}{totalPages > 1 && <View style={{ gap: 8 }}><AppText>{t("subscriptions:m_ba04e88c13f0", { value0: currentPage, value1: totalPages })}</AppText><AppButton title={t("subscriptions:m_b52d1a229ccf")} variant="secondary" disabled={currentPage <= 1} onPress={() => setPage(currentPage - 1)}/><AppButton title={t("subscriptions:m_4bd9f29d4bbd")} variant="secondary" disabled={currentPage >= totalPages} onPress={() => setPage(currentPage + 1)}/></View>}</> : <EmptyState title={t("subscriptions:m_0a83a3c1d4cf")} message={t("subscriptions:m_615e317726c3")}/>}
  </AppScreen>;
}
