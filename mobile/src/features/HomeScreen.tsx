import { t, useLocaleRevision } from "../i18n/runtime";
import { View } from 'react-native';
import { router } from 'expo-router';
import { Bell, Plus } from 'lucide-react-native';
import { AppScreen, AppText, AppButton, Brand, IconButton, AppCard, SectionHeader, LoadingSkeleton, EmptyState, MotionCard, styles } from '../components/ui';
import { useDashboard } from './payments/queries';
import { FinancialAmount, FinanceError, MonthlySummary, ProjectedRow } from './payments/components';
import { dateLabel, greeting } from '../utils/format';
import { SubscriptionDashboardCard } from './subscriptions/components';
export default function HomeScreen() {
    useLocaleRevision();
    const { clock, summary, upcoming, overdue, hasOverdueRange, refresh } = useDashboard();
    const add = () => router.push('/add-payment');
    const content = () => {
        if (clock.isError || summary.isError)
            return <FinanceError error={clock.error ?? summary.error} retry={() => { void refresh(); }}/>;
        if (!clock.data || !summary.data)
            return <LoadingSkeleton />;
        const data = summary.data;
        if (data.activePaymentPlanCount === 0 && data.paidPaymentCount === 0 && data.pendingPaymentCount === 0 && data.skippedPaymentCount === 0)
            return <EmptyState title={t("dashboard:m_720e8781912b")} message={t("dashboard:m_187bc29a9dcf")} action={t("dashboard:m_fc47d3ee96ea")} onPress={add}/>;
        return <>
      <AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{dateLabel(data.context.fromDate, true)} – {dateLabel(data.context.toDate, true)} · {data.context.timeZoneId}</AppText>
      {data.context.usedTimeZoneFallback && <AppText variant="caption" tone="warning" style={{ marginBottom: 12 }}>{t("dashboard:m_c33b5ecf8b44")}</AppText>}
      <MotionCard><MonthlySummary summary={data}/></MotionCard><View style={{ marginTop: 12 }}><AppButton title={t("dashboard:m_fd5e149b0c41")} variant="secondary" onPress={() => router.push('/analytics')}/></View>
      <View style={{ marginTop: 18 }}><AppButton title={t("dashboard:m_fc47d3ee96ea")} icon={Plus} onPress={add}/></View>
      <SectionHeader title={t("dashboard:m_c88d4e2ae324")}/>
      <AppCard>{data.paidTotalsByCurrency.length ? data.paidTotalsByCurrency.map((total) => <View key={total.currencyCode} style={[styles.row, { flexWrap: 'wrap', gap: 8, marginVertical: 6 }]}><AppText tone="muted">{total.currencyCode}</AppText><FinancialAmount amount={total.amount} currency={total.currencyCode}/></View>) : <AppText tone="muted">{t("dashboard:m_26f92ff4efd1")}</AppText>}<AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t("dashboard:m_48cce4a622e0", { value0: data.paidPaymentCount, value1: data.pendingPaymentCount, value2: data.skippedPaymentCount })}</AppText></AppCard>
      <SectionHeader title={t("dashboard:m_a918756a64ea")}/>
      <AppCard>{clock.data.nextUpcomingPayment ? <ProjectedRow item={clock.data.nextUpcomingPayment}/> : <AppText tone="muted">{t("dashboard:m_4a986bd03b55")}</AppText>}</AppCard>
      <SectionHeader title={t("dashboard:m_f550df9dcee0")}/>
      {!hasOverdueRange ? <AppCard><AppText tone="muted">{t("dashboard:m_4a7f5b58233f")}</AppText></AppCard> : overdue.isError ? <FinanceError error={overdue.error} retry={() => { void overdue.refetch(); }}/> : !overdue.data ? <LoadingSkeleton /> : <AppCard><AppText variant="caption" tone="warning">{t("dashboard:m_be0bd1466097", { value0: overdue.data.totalCount })}</AppText>{overdue.data.items.map((item) => <ProjectedRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} overdue/>)}{!overdue.data.totalCount && <AppText tone="muted" style={{ marginTop: 12 }}>{t("dashboard:m_28555f323d63")}</AppText>}{overdue.data.totalCount > 3 && <AppText variant="caption" tone="muted">{t("dashboard:m_6747ed989728")}</AppText>}</AppCard>}
      <SectionHeader title={t("dashboard:m_978b30c5e213")} action={t("dashboard:m_aa6a83bfcde1")} onPress={() => router.push('/(tabs)/payments')}/>
      {upcoming.isError ? <FinanceError error={upcoming.error} retry={() => { void upcoming.refetch(); }}/> : !upcoming.data ? <LoadingSkeleton /> : <AppCard><AppText variant="caption" tone="muted">{t("dashboard:m_a7770145884b", { value0: dateLabel(upcoming.data.context.fromDate), value1: dateLabel(upcoming.data.context.toDate), value2: upcoming.data.totalCount })}</AppText>{upcoming.data.items.map((item) => <ProjectedRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item}/>)}{!upcoming.data.totalCount && <AppText tone="muted" style={{ marginTop: 12 }}>{t("dashboard:m_1ffb3a1d64e7")}</AppText>}{upcoming.data.totalCount > 3 && <AppText variant="caption" tone="muted">{t("dashboard:m_6747ed989728")}</AppText>}</AppCard>}
    </>;
    };
    return <AppScreen refreshing={clock.isRefetching || summary.isRefetching || upcoming.isRefetching || overdue.isRefetching} onRefresh={() => { void refresh(); }}>
    <View style={[styles.row, { marginBottom: 28 }]}><Brand /><IconButton icon={Bell} label={t("dashboard:m_fc2cbca9acbe")} onPress={() => router.push('/notifications')}/></View>
    <AppText tone="muted">{greeting(new Date().getHours())}</AppText><AppText variant="title" accessibilityRole="header" style={{ marginTop: 4, marginBottom: 24 }}>{t("dashboard:m_3d89a3625605")}</AppText>{content()}<SubscriptionDashboardCard />
  </AppScreen>;
}
