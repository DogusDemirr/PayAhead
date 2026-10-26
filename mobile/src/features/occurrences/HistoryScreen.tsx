import { t, useLocaleRevision } from "../../i18n/runtime";
import { useState } from 'react';
import { View } from 'react-native';
import { AppScreen, AppHeader, AppButton, AppCard, AppText, EmptyState, LoadingSkeleton } from '../../components/ui';
import { useFinanceClock, useHistory } from '../payments/queries';
import { FinanceError } from '../payments/components';
import { monthRange } from '../payments/dates';
import type { OccurrenceStatus } from '../payments/contracts';
import { MonthNavigation } from './MonthNavigation';
import { OccurrenceRow } from './shared';
export default function HistoryScreen() {
    useLocaleRevision();
    const clock = useFinanceClock();
    if (clock.isError)
        return <AppScreen><AppHeader title={t("calendar:m_50636cefa8dc")} back/><FinanceError error={clock.error} retry={() => { void clock.refetch(); }}/></AppScreen>;
    if (!clock.data)
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    return <HistoryBody today={clock.data.context.today}/>;
}
export function HistoryBody({ today }: {
    today: string;
}) {
    useLocaleRevision();
    const [month, setMonth] = useState(today.slice(0, 7) + '-01');
    const [status, setStatus] = useState<OccurrenceStatus>('Paid');
    const [page, setPage] = useState(1);
    const range = monthRange(month);
    const end = range.toDate > today ? today : range.toDate;
    const query = useHistory(range.fromDate, end, status, page);
    return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title={t("calendar:m_50636cefa8dc")} subtitle={t("calendar:m_a993d56aacf7")} back/>
    <MonthNavigation month={month} maximum={today.slice(0, 7) + '-01'} onChange={value => { setMonth(value); setPage(1); }}/>
    <View style={{ gap: 8, marginBottom: 16 }}>{(['Paid', 'Skipped', 'Pending'] as const).map(value => <AppButton key={value} title={({ Paid: t("calendar:m_2f36f3362ead"), Skipped: t("calendar:m_594b1254c47d"), Pending: 'Bekliyor / gecikti' })[value] + (status === value ? ' ✓' : '')} variant={status === value ? 'primary' : 'secondary'} onPress={() => { setStatus(value); setPage(1); }}/>)}</View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 16 }}>{t("calendar:m_4d2f75ead23f")}</AppText>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }}/> : !query.data ? <LoadingSkeleton /> : <><AppText variant="caption" tone="muted">{t("calendar:m_3caac5efb49b", { value0: query.data.totalCount, value1: page, value2: Math.max(1, query.data.totalPages) })}</AppText>{query.data.items.length ? <AppCard>{query.data.items.map(item => <OccurrenceRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} today={today}/>)}</AppCard> : <EmptyState title={t("calendar:m_b231b2e3abdf")} message={t("calendar:m_b643b19b02e5")}/>}<View style={{ gap: 8, marginTop: 16 }}><AppButton title={t("calendar:m_b74566b03eea")} variant="secondary" disabled={page <= 1 || query.isFetching} onPress={() => setPage(p => p - 1)}/><AppButton title={t("calendar:m_4c68362c332e")} variant="secondary" disabled={page >= query.data.totalPages || query.isFetching} onPress={() => setPage(p => p + 1)}/></View></>}
  </AppScreen>;
}
