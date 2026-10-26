import { t, useLocaleRevision } from "../i18n/runtime";
import { useEffect, useState } from 'react';
import { View } from 'react-native';
import { Plus, SlidersHorizontal } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppInput, AppText, AppCard, AppButton, EmptyState, LoadingSkeleton, IconButton, styles } from '../components/ui';
import { useFinanceClock, usePaymentList } from './payments/queries';
import { FinanceError, PlanRow } from './payments/components';
import { nextPlanDate } from './payments/dates';
import { currencyCodes } from './payments/currencies';
export default function PaymentsScreen() {
    useLocaleRevision();
    const [search, setSearch] = useState('');
    const [applied, setApplied] = useState('');
    const [page, setPage] = useState(1);
    const [active, setActive] = useState<boolean | undefined>(undefined);
    const [filtersOpen, setFiltersOpen] = useState(false);
    const [currency, setCurrency] = useState('');
    const [category, setCategory] = useState('');
    const [extra, setExtra] = useState<{
        currencyCode?: string;
        category?: string;
    }>({});
    const [filterError, setFilterError] = useState('');
    useEffect(() => { const timer = setTimeout(() => { setApplied(search.trim()); setPage(1); }, 300); return () => clearTimeout(timer); }, [search]);
    const query = usePaymentList({ pageNumber: page, pageSize: 20, search: applied || undefined, isActive: active, ...extra });
    const clock = useFinanceClock();
    return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); void clock.refetch(); }}><AppHeader title={t("common:m_c40832ab139b")} subtitle={t("common:m_00fda1a8f061")} right={<IconButton icon={Plus} label={t("common:m_fc47d3ee96ea")} onPress={() => router.push('/add-payment')}/>}/>
    <AppInput label={t("common:m_ab071c7811fb")} placeholder={t("common:m_f3bb1ae02837")} value={search} onChangeText={setSearch} maxLength={150} autoCorrect={false}/>
    <View style={{ marginTop: 12 }}><AppButton title={t("common:m_849ff7f27107")} icon={SlidersHorizontal} variant="ghost" onPress={() => setFiltersOpen(!filtersOpen)}/></View>
    {filtersOpen && <AppCard style={{ gap: 14 }}><AppInput label={t("common:m_5991df187aaf")} placeholder={t("common:m_1062dae67545")} value={currency} onChangeText={setCurrency} maxLength={3} autoCapitalize="characters"/><AppInput label={t("common:m_19c537a16cf8")} placeholder={t("common:m_bd072c271289")} value={category} onChangeText={setCategory} maxLength={50}/>{!!filterError && <AppText tone="danger" accessibilityRole="alert">{filterError}</AppText>}<AppButton title={t("common:m_362c4c8a211f")} variant="secondary" onPress={() => {
                const code = currency.trim().toUpperCase();
                if (code && !currencyCodes.includes(code)) {
                    setFilterError(t("common:m_7c98416792c1"));
                    return;
                }
                setFilterError('');
                setExtra({ currencyCode: code || undefined, category: category.trim() || undefined });
                setPage(1);
                setFiltersOpen(false);
            }}/><AppButton title={t("common:m_28dc74dcd54b")} variant="ghost" onPress={() => { setCurrency(''); setCategory(''); setExtra({}); setFilterError(''); setPage(1); }}/></AppCard>}
    <View style={[styles.row, { gap: 8, marginVertical: 20 }]}>{[{ label: t("common:m_f43a65ad8268"), value: undefined }, { label: t("payments:m_d5f884547452"), value: true }, { label: t("payments:m_edd812578cbb"), value: false }].map((option) => <FilterChip key={option.label} label={option.label} selected={active === option.value} onPress={() => { setActive(option.value); setPage(1); }}/>)}</View>
    <AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{t("common:m_71605e3019a3")}</AppText>
    {!!(extra.currencyCode || extra.category) && <AppText variant="caption" tone="accentText" style={{ marginBottom: 12 }}>{extra.currencyCode ?? t("common:m_16c74bf5cc6f")} · {extra.category ?? t("common:m_22ae0cc71dfc")}</AppText>}
    {search.trim() !== applied || query.isPending ? <LoadingSkeleton /> : query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }}/> : query.data?.items.length ? <>
      <AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{t("common:m_3be6008529d7", { value0: query.data.totalCount, value1: query.data.pageNumber, value2: query.data.totalPages })}</AppText>
      <AppCard>{query.data.items.map((plan) => <PlanRow key={plan.id} plan={plan} next={clock.data ? nextPlanDate(plan, clock.data.context.today) : undefined}/>)}</AppCard>
      {clock.isError && <AppText tone="warning" variant="caption" style={{ marginTop: 12 }}>{t("common:m_5a0a2ac2ea70")}</AppText>}
      <AppText variant="caption" tone="muted" style={{ marginTop: 12 }}>{t("common:m_d6b86f56f585")}</AppText>
      <View style={[styles.row, { gap: 12, marginTop: 24 }]}><View style={{ flex: 1 }}><AppButton title={t("common:m_f1d48e85474e")} variant="secondary" disabled={page <= 1} onPress={() => setPage(page - 1)}/></View><View style={{ flex: 1 }}><AppButton title={t("common:m_ffe6922f5329")} variant="secondary" disabled={page >= query.data.totalPages} onPress={() => setPage(page + 1)}/></View></View>
    </> : <EmptyState title={page > 1 ? t("common:m_8522d5a985f5") : applied || active !== undefined || extra.currencyCode || extra.category ? t("common:m_1176f5007df5") : t("common:m_6709724982cc")} message={t("common:m_8bbeb6b2e32b")} action={page > 1 ? t("common:m_b74566b03eea") : t("common:m_fc47d3ee96ea")} onPress={() => page > 1 ? setPage(page - 1) : router.push('/add-payment')}/>}
  </AppScreen>;
}
function FilterChip({ label, selected, onPress }: {
    label: string;
    selected: boolean;
    onPress: () => void;
}) {
    useLocaleRevision();
    return <View style={{ flex: 1 }}><AppButton title={label + (selected ? ' ✓' : '')} variant={selected ? 'primary' : 'secondary'} onPress={onPress}/></View>;
}
