import { useAppDialog } from '../../components/dialogs';
import { t, useLocaleRevision } from "../../i18n/runtime";
import { CatalogIdentity } from '../catalog/CatalogIdentity';
import { useRef } from 'react';
import { View } from 'react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, AppButton, StatusBadge, LoadingSkeleton, styles } from '../../components/ui';
import { ApiError } from '../../services/api-error';
import { usePayment, usePaymentMutation, useFinanceClock } from './queries';
import { FinancialAmount, FinanceError } from './components';
import { dateLabel } from '../../utils/format';
import { nextPlanDate } from './dates';
import { frequencyLabels } from './validation';
import type { PaymentWrite } from './contracts';
export default function PaymentDetailScreen({ id, readOnly = false }: {
    id: string;
    readOnly?: boolean;
}) {
    const dialogs = useAppDialog();
    useLocaleRevision();
    const query = usePayment(id);
    const clock = useFinanceClock();
    const mutation = usePaymentMutation();
    const busy = useRef(false);
    const plan = query.data;
    const next = plan && clock.data ? nextPlanDate(plan, clock.data.context.today) : undefined;
    async function change(kind: 'delete' | 'deactivate') {
        if (!plan || busy.current)
            return;
        busy.current = true;
        try {
            if (kind === 'delete') {
                await mutation.mutateAsync({ kind: 'delete', id: plan.id, rowVersion: plan.rowVersion });
                router.dismissTo('/(tabs)/payments');
            }
            else {
                const body: PaymentWrite = { name: plan.name, description: plan.description, amount: plan.amount, currencyCode: plan.currencyCode, category: plan.category, frequency: plan.frequency, interval: plan.interval, startDate: plan.startDate, endDate: plan.endDate, anchorDay: plan.anchorDay, isActive: false };
                await mutation.mutateAsync({ kind: 'update', id: plan.id, rowVersion: plan.rowVersion, body });
            }
        }
        catch (error) {
            if (error instanceof ApiError && error.code === 'payment_changed') await query.refetch();
            throw error;
        }
        finally {
            busy.current = false;
        }
    }
    const blocked = mutation.isPending;
    return <AppScreen refreshing={query.isRefetching} onRefresh={() => { void query.refetch(); }}><AppHeader title={t("payments:m_7c72e97a2e24")} back/>
    {query.isPending ? <LoadingSkeleton /> : query.isError ? <FinanceError error={query.error} retry={() => { void query.refetch(); }}/> : plan ? <>
      <AppCard><StatusBadge label={plan.isActive ? t("payments:m_b31385e605a6") : t("payments:m_aff1d602e8dd")}/><AppText variant="title" style={{ marginTop: 18 }}>{plan.name}</AppText><View style={{ marginVertical: 20 }}><FinancialAmount amount={plan.amount} currency={plan.currencyCode} large/></View>
        <Detail label={t("payments:m_cd613d82a1a6")} value={plan.category}/><Detail label={t("payments:m_5da57b457b37")} value={t("payments:m_82413ba35131", { value0: frequencyLabels[plan.frequency], value1: plan.interval })}/><Detail label={t("payments:m_72373d3f7768")} value={dateLabel(plan.startDate, true, true)}/><Detail label={t("payments:m_b649bea15b42")} value={plan.endDate ? dateLabel(plan.endDate, true, true) : t("payments:m_8fd6cbabf5c7")}/><Detail label={t("payments:m_774dac727b5c")} value={plan.anchorDay === null ? t("payments:m_4ac8ab998cc8") : String(plan.anchorDay)}/><Detail label={t("payments:m_01e23f52e395")} value={next === undefined ? t("payments:m_cacd6a7dd4d4") : next ? dateLabel(next, true, true) : t("payments:m_1b8b6498c5f1")}/>
        <AppText variant="caption" tone="muted" style={{ marginTop: 14 }}>{t("payments:m_c7efa547ceea")}</AppText>{plan.description && <AppText tone="muted" style={{ marginTop: 20 }}>{plan.description}</AppText>}
      </AppCard>
      <CatalogIdentity categoryId={plan.catalogCategoryId} serviceId={plan.catalogServiceId} fallback={plan.category}/><View style={{ marginTop: 16 }}><AppButton title={t("payments:m_d382a5a56146")} variant="secondary" onPress={() => router.push({ pathname: '/reminders', params: { id: plan.id } })}/></View>
      {readOnly ? <AppCard style={{ marginTop: 18 }}><AppText tone="muted">{t("payments:m_432ffe87483d")}</AppText></AppCard> : <View style={{ gap: 12, marginTop: 24 }}><AppButton title={t("payments:m_46e308473e94")} disabled={blocked} onPress={() => router.push({ pathname: '/edit-payment', params: { id: plan.id } })}/>
        {plan.isActive && <AppButton title={t("payments:m_41bb7b55f9b1")} variant="secondary" disabled={blocked} onPress={() => dialogs.alert(t("common:m_f4ba3168e4d4"), t("common:m_d9a435bdbef5"), [{ text: t("payments:m_39a8d28c1c6b"), style: 'cancel' }, { text: t("payments:m_5d59500b0808"), onPress: () => change('deactivate') }])}/>}
        <AppButton title={t("payments:m_8546b2337a9a")} variant="ghost" disabled={blocked} onPress={() => dialogs.alert(t("common:m_9c62bf8ff200"), t("payments:m_aafd5445c85f", { value0: plan.name }), [{ text: t("payments:m_39a8d28c1c6b"), style: 'cancel' }, { text: t("payments:m_f698e9b40f9f"), style: 'destructive', onPress: () => change('delete') }])}/>
      </View>}
    </> : null}
  {dialogs.dialog}</AppScreen>;
}
function Detail({ label, value }: {
    label: string;
    value: string;
}) { useLocaleRevision(); return <View style={[styles.row, { gap: 14, alignItems: 'flex-start', marginTop: 14 }]}><AppText tone="muted" style={{ flex: 1 }}>{label}</AppText><AppText variant="label" style={{ flex: 1, textAlign: 'right' }}>{value}</AppText></View>; }
