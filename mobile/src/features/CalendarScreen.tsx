import { t, useLocaleRevision } from "../i18n/runtime";
import { useState } from 'react';
import { Pressable, View } from 'react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, EmptyState, LoadingSkeleton, SectionHeader } from '../components/ui';
import { useCalendar, useFinanceClock } from './payments/queries';
import { FinanceError } from './payments/components';
import { dayKey, daysInMonth } from './payments/dates';
import { localCalendarDate } from '../components/date-field';
import { MonthNavigation } from './occurrences/MonthNavigation';
import { OccurrenceRow, statusLabel } from './occurrences/shared';
import { useTheme } from '../theme/use-theme';
import { dateLabel } from '../utils/format';
export default function CalendarScreen() {
    useLocaleRevision();
    const clock = useFinanceClock();
    if (clock.isError)
        return <AppScreen><AppHeader title={t("calendar:m_5785a01da6e0")}/><FinanceError error={clock.error} retry={() => { void clock.refetch(); }}/></AppScreen>;
    if (!clock.data)
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    return <CalendarBody today={clock.data.context.today}/>;
}
export function CalendarBody({ today }: {
    today: string;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    const [month, setMonth] = useState(today.slice(0, 7) + '-01');
    const [selected, setSelected] = useState(today);
    const [year, number] = month.split('-').map(Number);
    const query = useCalendar(year, number);
    const days = query.data?.days ?? [];
    const count = daysInMonth(year, number);
    const offset = (localCalendarDate(month).getDay() + 6) % 7;
    function selectMonth(value: string) { setMonth(value); setSelected(value); }
    return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title={t("calendar:m_5785a01da6e0")} subtitle={t("calendar:m_4d83791ea31c")}/>
    <View style={{ gap: 8, marginBottom: 16 }}><AppButton title={t("calendar:m_50636cefa8dc")} variant="secondary" onPress={() => router.push('/payment-history')}/><AppButton title={t("calendar:m_059b7d11ff43")} variant="ghost" onPress={() => { setMonth(today.slice(0, 7) + '-01'); setSelected(today); }}/></View>
    <AppCard style={{ paddingHorizontal: 8 }}><MonthNavigation month={month} onChange={selectMonth}/>
      <View style={{ flexDirection: 'row' }}>{[t("calendar:m_4c5c101be403"), t("calendar:m_59cc0fc7e93e"), t("calendar:m_6d46ddd615db"), t("calendar:m_89ac4dc9eddf"), t("calendar:m_5f9967713ae0"), t("calendar:m_36986d15c759"), t("calendar:m_3b4ca471d9af")].map(label => <AppText key={label} variant="caption" tone="muted" style={{ width: '14.2857%', textAlign: 'center' }}>{label}</AppText>)}</View>
      <View style={{ flexDirection: 'row', flexWrap: 'wrap' }}>{Array.from({ length: offset + count }, (_, i) => {
            const day = i - offset + 1;
            if (day < 1)
                return <View key={i} style={{ width: '14.2857%', height: 58 }}/>;
            const key = dayKey(year, number, day);
            const entries = days.find(d => d.date === key)?.payments ?? [];
            const labels = [...new Set(entries.map(p => statusLabel(p.status, p.dueDate, today)))];
            return <Pressable key={key} accessibilityRole="button" accessibilityLabel={t("calendar:m_5117f9601eb1", { value0: key, value1: key === today ? t("calendar:m_58e8caab760d") : '', value2: labels.length ? ', ' + labels.join(', ') : '' })} accessibilityState={{ selected: selected === key }} onPress={() => setSelected(key)} style={{ width: '14.2857%', minHeight: 58, alignItems: 'center', justifyContent: 'center', gap: 4 }}><View style={{ borderRadius: 12, width: 36, height: 36, justifyContent: 'center', alignItems: 'center', backgroundColor: selected === key ? colors.navy : 'transparent', borderWidth: key === today ? 2 : 0, borderColor: colors.accentText }}><AppText tone={selected === key ? 'onNavy' : 'text'}>{day}</AppText></View><View style={{ flexDirection: 'row', gap: 2 }}>{labels.map(label => <View key={label} style={{ width: 5, height: 5, borderRadius: 3, backgroundColor: label === t("calendar:m_2f36f3362ead") ? colors.success : label === t("calendar:m_594b1254c47d") ? colors.muted : label === t("calendar:m_bffd1dfbfbed") ? colors.danger : colors.accentText }}/>)}</View></Pressable>;
        })}</View><AppText variant="caption" tone="muted" style={{ padding: 8 }}>{t("calendar:m_0cf0ee99c1d9")}</AppText></AppCard>
    <SectionHeader title={dateLabel(selected, true, true)}/>
    {query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }}/> : !query.data ? <LoadingSkeleton /> : <>{query.data.context.usedTimeZoneFallback && <AppText tone="warning">{t("calendar:m_9e98d8624610")}</AppText>}{days.find(d => d.date === selected)?.payments.length ? <AppCard>{days.find(d => d.date === selected)!.payments.map(item => <OccurrenceRow key={`${item.paymentPlanId}-${item.dueDate}`} item={item} today={query.data.context.today}/>)}</AppCard> : <EmptyState title={t("calendar:m_840a9f0b1c3a")} message={t("calendar:m_f0c374b5f4a5")}/>}</>}
  </AppScreen>;
}
