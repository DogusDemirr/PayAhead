import { t, useLocaleRevision } from "../../i18n/runtime";
import { useRef, useState } from 'react';
import { Switch, View } from 'react-native';
import { router } from 'expo-router';
import { isCancel } from 'axios';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AppScreen, AppHeader, AppInput, AppButton, AppText, AppCard, LoadingSkeleton, styles } from '../../components/ui';
import { ApiError } from '../../services/api-error';
import { useFinanceClock, usePayment, usePaymentMutation, useEntitlements } from './queries';
import { FinanceError, LimitNotice } from './components';
import { paymentFormSchema, formDefaults, toPaymentWrite, frequencyLabels, type PaymentFields } from './validation';
import { DateField } from '../../components/date-field';
import type { PaymentPlan, Entitlement } from './contracts';
import { CatalogSelectionField } from '../catalog/CatalogPicker';
export default function PaymentFormScreen({ id }: {
    id?: string;
}) {
    useLocaleRevision();
    const query = usePayment(id);
    const clock = useFinanceClock();
    const entitlements = useEntitlements();
    const [revision, setRevision] = useState(0);
    if (clock.isError || (id && query.isError))
        return <AppScreen><AppHeader title={id ? t("payments:m_46e308473e94") : t("payments:m_7e31f8204d5e")} back/><FinanceError error={clock.error ?? query.error} retry={() => {
                void clock.refetch();
                if (id)
                    void query.refetch();
            }}/></AppScreen>;
    if (!clock.data || (id && !query.data))
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    return <PaymentForm key={`${id ?? 'new'}-${revision}`} plan={id ? query.data : undefined} today={clock.data.context.today} activeCount={clock.data.activePaymentPlanCount} entitlement={entitlements.data} entitlementError={entitlements.isError} reload={async () => {
            const result = await query.refetch();
            if (result.isSuccess)
                setRevision((value) => value + 1);
        }}/>;
}
export function PaymentForm({ plan, today, activeCount, entitlement, entitlementError, reload }: {
    plan?: PaymentPlan;
    today: string;
    activeCount: number;
    entitlement?: Entitlement;
    entitlementError?: boolean;
    reload: () => Promise<void>;
}) {
    useLocaleRevision();
    // Freeze the version being edited. Background refetch must not silently replace it.
    const [original] = useState(plan);
    const busy = useRef(false);
    const mutation = usePaymentMutation();
    const [failure, setFailure] = useState<unknown>(null);
    const [conflict, setConflict] = useState(false);
    const [uncertain, setUncertain] = useState(false);
    const { control, setValue, setError, handleSubmit, formState: { errors } } = useForm<PaymentFields>({ resolver: zodResolver(paymentFormSchema), defaultValues: formDefaults(today, original) });
    const startDate = useWatch({ control, name: 'startDate' });
    const categoryId = useWatch({ control, name: 'catalogCategoryId' });
    const serviceId = useWatch({ control, name: 'catalogServiceId' });
    const category = useWatch({ control, name: 'category' });
    const selectedName = useWatch({ control, name: 'name' });
    const [catalogError, setCatalogError] = useState('');
    const frequency = useWatch({ control, name: 'frequency' });
    const active = useWatch({ control, name: 'isActive' });
    const limitReached = !!entitlement && activeCount >= entitlement.limits.activePaymentPlans && !original?.isActive;
    const submit = () => handleSubmit(async (fields) => {
        if (busy.current || conflict || uncertain)
            return;
        if (!original && !fields.catalogCategoryId) {
            setCatalogError(t("payments:m_05fbb278ab68"));
            return;
        }
        busy.current = true;
        setFailure(null);
        try {
            const body = toPaymentWrite(fields);
            const result = await mutation.mutateAsync(original ? { kind: 'update', id: original.id, rowVersion: original.rowVersion, body } : { kind: 'create', body });
            if (result) {
                const target = { pathname: '/payment' as const, params: { id: result.id } };
                if (original)
                    router.dismissTo(target);
                else
                    router.replace(target);
            }
        }
        catch (error) {
            if (!isCancel(error)) {
                setFailure(error);
                if (error instanceof ApiError) {
                    if (error.status === 409)
                        setConflict(true);
                    if (error.kind === 'network' || error.kind === 'timeout')
                        setUncertain(true);
                    for (const name of Object.keys(formDefaults(today)) as (keyof PaymentFields)[]) {
                        const messages = error.errors?.[name] ?? error.errors?.[name[0].toUpperCase() + name.slice(1)];
                        if (Array.isArray(messages) && messages.length)
                            setError(name, { type: 'server', message: messages.join(' ') });
                    }
                }
            }
        }
        finally {
            busy.current = false;
        }
    })();
    const input = (name: Exclude<keyof PaymentFields, 'isActive' | 'frequency' | 'catalogCategoryId' | 'catalogServiceId'>, label: string, options: {
        numeric?: boolean;
        placeholder?: string;
        maxLength?: number;
        multiline?: boolean;
    } = {}) => <Controller key={name} control={control} name={name} render={({ field }) => <AppInput label={label} value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors[name]?.message} placeholder={options.placeholder} maxLength={options.maxLength} multiline={options.multiline} keyboardType={name === 'amount' ? 'decimal-pad' : options.numeric ? 'number-pad' : 'default'} autoCapitalize={name === 'currencyCode' ? 'characters' : 'sentences'} editable={!mutation.isPending && !conflict && !uncertain}/>}/>;
    return <AppScreen keyboard><AppHeader title={original ? t("payments:m_46e308473e94") : t("payments:m_7e31f8204d5e")} subtitle={t("payments:m_50cea4c0f664")} back/>
    {entitlement && <AppText variant="caption" tone="muted" style={{ marginBottom: 16 }}>{t("payments:m_e4d8d83eef36", { value0: entitlement.plan, value1: activeCount, value2: entitlement.limits.activePaymentPlans })}</AppText>}
    {(limitReached || failure instanceof ApiError && failure.code === 'entitlement_limit') && <LimitNotice />}
    {entitlementError && <AppText tone="warning" variant="caption" style={{ marginBottom: 16 }}>{t("payments:m_51929427e17d")}</AppText>}
    <View style={{ gap: 20 }}>
      <CatalogSelectionField mode="Payment" initialOpen={!original} categoryId={categoryId} serviceId={serviceId} legacyCategory={category || undefined} error={catalogError || errors.catalogCategoryId?.message || errors.catalogServiceId?.message || errors.category?.message} disabled={mutation.isPending || conflict || uncertain} onSelect={selection => {
            setCatalogError('');
            setValue('catalogCategoryId', selection.category.id);
            setValue('catalogServiceId', selection.service?.id ?? null);
            setValue('category', selection.category.nameTr);
            if (selection.service) {
                setValue('name', selection.service.displayName);
                setValue('frequency', selection.service.defaultRecurrenceType ?? 'Once');
                setValue('anchorDay', '');
            }
            else if (serviceId)
                setValue('name', '');
        }}/>
      {!serviceId || original ? input('name', t("common:m_78b46a64eb5e"), { maxLength: 150, placeholder: t("payments:m_2b98db0ebe2f") }) : <AppText variant="section">{selectedName}</AppText>}
      {input('amount', t("payments:m_c15a0321c925"), { placeholder: '899,90' })}
<AppText variant="caption" tone="muted">{t("common:amountInputHint")}</AppText>
      {input('currencyCode', t("common:m_0c9e12052e29"), { maxLength: 3, placeholder: 'TRY / USD / EUR' })}
      {original && !categoryId && input('category', t("payments:m_cd613d82a1a6"), { maxLength: 50 })}
      <AppText variant="label">{t("payments:m_d13255fd6d45")}</AppText>
      <Controller control={control} name="frequency" render={({ field }) => <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>{(Object.keys(frequencyLabels) as (keyof typeof frequencyLabels)[]).map((value) => <View key={value} style={{ minWidth: '45%', flexGrow: 1 }}><AppButton title={frequencyLabels[value] + (value === field.value ? ' ✓' : '')} variant={value === field.value ? 'primary' : 'secondary'} disabled={mutation.isPending || conflict} onPress={() => {
                    field.onChange(value);
                    if (!['Monthly', 'Yearly'].includes(value))
                        setValue('anchorDay', '');
                }}/></View>)}</View>}/>
      {frequency !== 'Once' && input('interval', t("common:m_dfdb7b4f231e"), { numeric: true, placeholder: '1' })}
      <Controller control={control} name="startDate" render={({ field }) => <DateField label={t("payments:m_c2cc4ed0e0de")} value={field.value} onChange={field.onChange} fallback={today} disabled={mutation.isPending || conflict} error={errors.startDate?.message}/>}/>
      <Controller control={control} name="endDate" render={({ field }) => <DateField label={t("payments:m_edd225f78b1c")} value={field.value} onChange={field.onChange} fallback={startDate || today} minimum={startDate} optional disabled={mutation.isPending || conflict} error={errors.endDate?.message}/>}/>
      {['Monthly', 'Yearly'].includes(frequency) && <>{input('anchorDay', t("common:m_62cae61c31f3"), { numeric: true, maxLength: 2, placeholder: t("payments:m_f3c0ae536e6c") })}<AppCard><AppText variant="caption" tone="muted">{t("payments:m_9c6457504337")}</AppText></AppCard></>}
      {input('description', t("common:m_9f4ce70fe7c2"), { maxLength: 2000, multiline: true })}
      <Controller control={control} name="isActive" render={({ field }) => <View style={[styles.row, { minHeight: 60 }]}><View style={{ flex: 1 }}><AppText variant="label">{t("payments:m_b31385e605a6")}</AppText><AppText variant="caption" tone="muted">{t("payments:m_12bb0cfdd296")}</AppText></View><Switch accessibilityLabel={t("payments:m_b31385e605a6")} value={field.value} disabled={mutation.isPending || conflict} onValueChange={field.onChange} style={{ minHeight: 48 }}/></View>}/>
      {!!failure && <AppCard><AppText accessibilityRole="alert" tone="danger">{failure instanceof ApiError ? failure.message : t("payments:m_df44192b41ac")}</AppText>{conflict && <View style={{ marginTop: 12 }}><AppText variant="caption" tone="muted" style={{ marginBottom: 12 }}>{t("payments:m_97aa3193e40c")}</AppText><AppButton title={t("payments:m_40e816c987a2")} variant="secondary" onPress={() => { void reload(); }}/></View>}</AppCard>}
      {uncertain && <AppCard><AppText tone="warning">{t("payments:m_e7bbbd13fda6")}</AppText><View style={{ marginTop: 12, gap: 12 }}><AppButton title={t("payments:m_42a396b3c3dd")} variant="secondary" onPress={() => router.push('/(tabs)/payments')}/><AppButton title={t("payments:m_c6e47849be91")} variant="ghost" onPress={() => setUncertain(false)}/></View></AppCard>}
      <AppButton title={original ? t("payments:m_f63044f8dff1") : t("payments:m_6c7d3be05e89")} loading={mutation.isPending} disabled={mutation.isPending || conflict || uncertain || active && limitReached} onPress={submit}/>
      <AppText variant="caption" tone="muted">{t("payments:m_9a4f23eb644c")}</AppText>
    </View>
  </AppScreen>;
}
