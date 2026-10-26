import { t, useLocaleRevision } from "../../i18n/runtime";
import { useState } from 'react';
import { View } from 'react-native';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, AppInput, SectionHeader, EmptyState, LoadingSkeleton } from '../../components/ui';
import { useFinanceClock, useAnalytics } from '../payments/queries';
import { FinanceError, FinancialAmount } from '../payments/components';
import { MonthNavigation } from '../occurrences/MonthNavigation';
import { AmountChart, StatusChart } from './Charts';
import type { CurrencyAnalytics } from '../payments/contracts';
import { dateLabel } from '../../utils/format';
import { currencyCodes } from '../payments/currencies';
export default function AnalyticsScreen() {
    useLocaleRevision();
    const clock = useFinanceClock();
    if (clock.isError)
        return <AppScreen><AppHeader title={t("analytics:m_34db704cad42")} back/><FinanceError error={clock.error} retry={() => { void clock.refetch(); }}/></AppScreen>;
    if (!clock.data)
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    return <AnalyticsBody today={clock.data.context.today}/>;
}
export function AnalyticsBody({ today }: {
    today: string;
}) {
    useLocaleRevision();
    const [month, setMonth] = useState(today.slice(0, 7) + '-01');
    const [currency, setCurrency] = useState('TRY');
    const [filter, setFilter] = useState('');
    const [applied, setApplied] = useState<string>();
    const [filterError, setFilterError] = useState('');
    const [year, number] = month.split('-').map(Number);
    const query = useAnalytics(year, number, applied);
    const selected = query.data?.currencies.find(c => c.currencyCode === currency) ?? query.data?.currencies[0];
    return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title={t("analytics:m_34db704cad42")} subtitle={t("analytics:m_0694bb658290")} back/><MonthNavigation month={month} onChange={value => { if (value >= '0001-06-01' && value <= '9999-12-01')
        setMonth(value); }}/>
    <View style={{ gap: 8, marginBottom: 16 }}><AppInput label={t("analytics:m_b12a04091838")} placeholder={t("analytics:m_6e567ddfeb02")} value={filter} onChangeText={setFilter} maxLength={3} autoCapitalize="characters" error={filterError || undefined}/><AppButton title={t("analytics:m_7f441105ea24")} variant="secondary" onPress={() => { const code = filter.trim().toUpperCase(); if (code && !currencyCodes.includes(code)) {
        setFilterError(t("analytics:m_9ca92bf2de8d"));
        return;
    } setFilterError(''); setApplied(code || undefined); if (code)
        setCurrency(code); }}/></View>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }}/> : !query.data ? <LoadingSkeleton /> : !selected ? <EmptyState title={t("analytics:m_554c13c8e0dc")} message={t("analytics:m_dde3c601ccef")}/> : <><View style={{ gap: 8, marginBottom: 16 }}>{query.data.currencies.map(c => <AppButton key={c.currencyCode} title={c.currencyCode + (c.currencyCode === selected.currencyCode ? ' ✓' : '')} variant={c.currencyCode === selected.currencyCode ? 'primary' : 'secondary'} onPress={() => setCurrency(c.currencyCode)}/>)}</View><AppText variant="caption" tone="muted">{dateLabel(query.data.context.fromDate, false, true)} – {dateLabel(query.data.context.toDate, false, true)} · {query.data.context.timeZoneId}</AppText><AppText variant="caption" tone="muted" style={{ marginVertical: 12 }}>{t("analytics:m_8e5f3bed5189")}</AppText>{query.data.context.usedTimeZoneFallback && <AppText tone="warning">{t("analytics:m_9e98d8624610")}</AppText>}<CurrencyAnalysis data={selected}/></>}
  </AppScreen>;
}
export function CurrencyAnalysis({ data }: {
    data: CurrencyAnalytics;
}) {
    useLocaleRevision();
    const current = data.months[data.months.length - 1];
    if (!current)
        return <EmptyState title={t("analytics:m_554b441847dd")}/>;
    const totals = [{ label: t("analytics:m_086522a2222f"), amount: current.plannedAmount }, { label: t("analytics:m_432698bc04fb"), amount: current.paidAmount }, { label: t("analytics:m_6da9e452fcbc"), amount: current.pendingAmount }, { label: t("analytics:m_982cf9cb2005"), amount: current.overdueAmount }];
    return <><AppCard>{totals.map(row => <View key={row.label} style={{ gap: 6, marginVertical: 10 }}><AppText tone="muted">{row.label}</AppText><FinancialAmount amount={row.amount} currency={data.currencyCode}/></View>)}</AppCard>
    <SectionHeader title={t("analytics:m_8d285b551104")}/><AppCard><AmountChart rows={data.months.map(m => ({ label: dateLabel(m.month, false, true), amount: m.paidAmount }))} currency={data.currencyCode}/><AppText variant="caption" tone="muted" style={{ marginTop: 16 }}>{t("analytics:m_9fef2b0f0a4d")}</AppText></AppCard>
    <SectionHeader title={t("analytics:m_c86cd1a1d946")}/><AppCard><AmountChart rows={data.months.map(m => ({ label: dateLabel(m.month, false, true), amount: m.plannedAmount }))} currency={data.currencyCode}/></AppCard>
    <SectionHeader title={t("analytics:m_3864edd32cbf")}/><AppCard><StatusChart paid={current.paidCount} pending={current.pendingCount} skipped={current.skippedCount}/></AppCard>
    <SectionHeader title={t("analytics:m_e8bd0861623e")}/>{data.paidCategories.length ? <AppCard><AmountChart rows={data.paidCategories.slice(0, 8)} currency={data.currencyCode}/>{data.paidCategories.length > 8 && <AppText variant="caption" tone="muted">{t("analytics:m_582f83600904", { value0: data.paidCategories.length })}</AppText>}</AppCard> : <AppCard><AppText tone="muted">{t("analytics:m_f607b1237936")}</AppText></AppCard>}
    <SectionHeader title={t("analytics:m_85a931fa2f9f")}/><AppCard><AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{t("analytics:m_2219dab1c111")}</AppText>{data.topPlans.map((plan, i) => <View key={`${plan.label}-${i}`} style={{ marginVertical: 8, gap: 4 }}><AppText variant="label">{plan.label}</AppText><FinancialAmount amount={plan.amount} currency={data.currencyCode}/><AppText variant="caption" tone="muted">{t("analytics:m_6d38d0719d06", { value0: plan.count })}</AppText></View>)}</AppCard>
  </>;
}
