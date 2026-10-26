import { currentLocale, t, useLocaleRevision } from '../../i18n/runtime';
import { useRef, useState } from 'react';
import { View } from 'react-native';
import { isCancel } from 'axios';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppText, AppCard, AppButton, AppInput, LoadingSkeleton, StatusBadge } from '../../components/ui';
import { usePayment, useOccurrence, useOccurrenceMutation, useFinanceClock } from '../payments/queries';
import { FinancialAmount, FinanceError } from '../payments/components';
import type { Occurrence, OccurrenceStatus, PaymentPlan } from '../payments/contracts';
import { canonicalAmount } from '../payments/money';
import { dateLabel } from '../../utils/format';
import { ApiError } from '../../services/api-error';
import { statusLabel } from './shared';
export default function OccurrenceScreen({ id, date }: {
    id: string;
    date: string;
}) {
    useLocaleRevision();
    const plan = usePayment(id);
    const occurrence = useOccurrence(id, date);
    const clock = useFinanceClock();
    const [revision, setRevision] = useState(0);
    const reload = async () => { const result = await occurrence.refetch(); if (result.isSuccess) {
        await plan.refetch();
        setRevision(r => r + 1);
    } };
    if (plan.isError || occurrence.isError || clock.isError)
        return <AppScreen><AppHeader title={t("calendar:m_52ba82f41da1")} back/><FinanceError error={plan.error ?? occurrence.error ?? clock.error} retry={() => { void reload(); void clock.refetch(); }}/></AppScreen>;
    if (!plan.data || !occurrence.data || !clock.data)
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    return <OccurrenceBody key={revision} plan={plan.data} initial={occurrence.data} today={clock.data.context.today} reload={reload}/>;
}
export function OccurrenceBody({ plan, initial, today, reload }: {
    plan: PaymentPlan;
    initial: Occurrence;
    today: string;
    reload: () => Promise<void>;
}) {
    useLocaleRevision();
    const [record, setRecord] = useState(initial);
    const [amount, setAmount] = useState(initial.actualAmount ?? plan.amount);
    const mutation = useOccurrenceMutation();
    const busy = useRef(false);
    const [error, setError] = useState<unknown>(null);
    const [blocked, setBlocked] = useState(false);
    const [success, setSuccess] = useState(false);
    async function change(status: OccurrenceStatus) {
        if (busy.current || blocked)
            return;
        if (status === 'Paid' && !canonicalAmount(amount)) {
            setError(new ApiError(t("calendar:m_25e24627a6ee")));
            return;
        }
        busy.current = true;
        setError(null);
        setSuccess(false);
        try {
            const result = await mutation.mutateAsync({ id: plan.id, date: record.dueDate, status, rowVersion: record.rowVersion, amount });
            setRecord(result);
            setSuccess(true);
        }
        catch (failure) {
            if (!isCancel(failure)) {
                setError(failure);
                if (failure instanceof ApiError && (failure.status === 409 || failure.kind === 'network' || failure.kind === 'timeout'))
                    setBlocked(true);
            }
        }
        finally {
            busy.current = false;
        }
    }
    return <AppScreen keyboard><AppHeader title={t("calendar:m_52ba82f41da1")} subtitle={plan.name} back/><View style={{ gap: 16 }}>
    <AppCard><AppText tone="muted">{t("calendar:m_186683421cb0")}</AppText><AppText variant="section">{dateLabel(record.dueDate, true, true)}</AppText><View style={{ marginVertical: 14 }}><StatusBadge label={statusLabel(record.status, record.dueDate, today)} tone={record.status === 'Paid' ? 'success' : record.status === 'Skipped' ? 'muted' : 'warning'}/></View><AppText tone="muted">{t("calendar:m_291dca1a2d0c")}</AppText><FinancialAmount amount={plan.amount} currency={plan.currencyCode}/><AppText variant="caption" tone="muted" style={{ marginTop: 8 }}>{t("calendar:m_b49835f3551b")}</AppText>
      {record.actualAmount !== null && <View style={{ marginTop: 16 }}><AppText tone="muted">{t("calendar:m_ec38317781ca")}</AppText><FinancialAmount amount={record.actualAmount} currency={plan.currencyCode}/></View>}
      {record.paidAtUtc && <AppText style={{ marginTop: 12 }}>{t("calendar:m_1fda86a53e46", { value0: new Date(record.paidAtUtc).toLocaleString(currentLocale()) })}</AppText>}
    </AppCard>
    <AppButton title={t("calendar:m_a258e497496d")} variant="secondary" onPress={() => router.push({ pathname: '/payment', params: { id: plan.id } })}/>
    {plan.isActive ? <><AppInput label={t("calendar:m_77f23e1b9711")} value={amount} onChangeText={setAmount} keyboardType="decimal-pad" editable={!mutation.isPending && !blocked}/><AppText variant="caption" tone="muted">{t("calendar:m_fdaf91355c38")}</AppText>
      <AppButton title={t("calendar:m_fac7f884c235")} loading={mutation.isPending} disabled={blocked} onPress={() => { void change('Paid'); }}/><AppButton title={t("calendar:m_c79dbe3338b9")} variant="secondary" disabled={mutation.isPending || blocked} onPress={() => { void change('Skipped'); }}/><AppButton title={t("calendar:m_c1db7b38a01d")} variant="ghost" disabled={mutation.isPending || blocked || !record.isRecorded} onPress={() => { void change('Pending'); }}/></> : <AppText tone="muted">{t("calendar:m_5c1120aa3f2a")}</AppText>}
    {success && <AppText accessibilityRole="alert" tone="success">{t("calendar:m_e41da48c42b3")}</AppText>}
    {!!error && <AppCard><AppText tone="danger" accessibilityRole="alert">{error instanceof ApiError ? error.message : t("calendar:m_643f2c5b4ff3")}</AppText>{blocked && <><AppText variant="caption" tone="muted" style={{ marginVertical: 12 }}>{t("calendar:m_2aeed2ec24bc")}</AppText><AppButton title={t("calendar:m_a1ee8d6f35e4")} onPress={() => { void reload(); }} variant="secondary"/></>}</AppCard>}
  </View></AppScreen>;
}
