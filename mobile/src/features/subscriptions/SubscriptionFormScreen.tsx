import { t, useLocaleRevision } from "../../i18n/runtime";
import { useRef, useState } from 'react';
import { Switch, View } from 'react-native';
import { router } from 'expo-router';
import { isCancel } from 'axios';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AppScreen, AppHeader, AppText, AppInput, AppButton, AppCard, LoadingSkeleton, styles } from '../../components/ui';
import { DateField } from '../../components/date-field';
import { FinanceError } from '../payments/components';
import { useEntitlements, useFinanceClock } from '../payments/queries';
import type { Entitlement } from '../payments/contracts';
import { ApiError } from '../../services/api-error';
import { useSubscription, useSubscriptionMutation, useSubscriptionSummary } from './queries';
import { subscriptionDefaults, subscriptionFormSchema, toSubscriptionWrite, consumesTrackingSlot, billingLabels, type SubscriptionFields } from './validation';
import type { Subscription } from './contracts';
import { CatalogSelectionField } from '../catalog/CatalogPicker';
import { SubscriptionLimitNotice, MutationFailure } from './components';
export default function SubscriptionFormScreen({ id }: {
    id?: string;
}) {
    useLocaleRevision();
    const detail = useSubscription(id);
    const summary = useSubscriptionSummary();
    const clock = useFinanceClock();
    const entitlement = useEntitlements();
    const [revision, setRevision] = useState(0);
    const reload = async () => { if (!id) {
        router.dismissTo('/(tabs)/subscriptions');
        return;
    } const result = await detail.refetch(); if (result.isSuccess)
        setRevision(n => n + 1); };
    if (summary.isError || clock.isError || id && detail.isError)
        return <AppScreen><AppHeader title={t("subscriptions:m_6c57e9a1d5bc")} back/><FinanceError error={summary.error ?? clock.error ?? detail.error} retry={() => { void summary.refetch(); void clock.refetch(); if (id)
            void detail.refetch(); }}/></AppScreen>;
    if (!summary.data || !clock.data || id && !detail.data)
        return <AppScreen><LoadingSkeleton /></AppScreen>;
    if (id && detail.data && !['Active', 'Trial'].includes(detail.data.status))
        return <AppScreen><AppHeader title={t("subscriptions:m_a77cb430aae5")} back/><AppCard><AppText>{t("subscriptions:m_9db9e4ffd519")}</AppText></AppCard></AppScreen>;
    return <SubscriptionForm key={`${id ?? 'new'}-${revision}`} record={id ? detail.data : undefined} today={summary.data.context.today} trackingCount={summary.data.activeSubscriptionCount + summary.data.trialSubscriptionCount} paymentCount={clock.data.activePaymentPlanCount} entitlement={entitlement.data} reload={reload}/>;
}
export function SubscriptionForm({ record, today, trackingCount, paymentCount, entitlement, reload }: {
    record?: Subscription;
    today: string;
    trackingCount: number;
    paymentCount: number;
    entitlement?: Entitlement;
    reload: () => Promise<void>;
}) {
    useLocaleRevision();
    const [original] = useState(record);
    const busy = useRef(false);
    const mutation = useSubscriptionMutation();
    const [failure, setFailure] = useState<unknown>(null);
    const [blocked, setBlocked] = useState(false);
    const { control, setValue, setError, handleSubmit, formState: { errors } } = useForm<SubscriptionFields>({ resolver: zodResolver(subscriptionFormSchema), defaultValues: subscriptionDefaults(today, original) });
    const values = useWatch({ control });
    const status = values.status;
    const frequency = values.billingFrequency;
    const [catalogError, setCatalogError] = useState('');
    const needsTracking = !original && consumesTrackingSlot(values as SubscriptionFields, today);
    const trackingLimit = !!entitlement && needsTracking && trackingCount >= entitlement.limits.activeTrackedSubscriptions;
    const paymentLimit = !!entitlement && !!values.autoRenew && !original?.autoRenew && paymentCount >= entitlement.limits.activePaymentPlans;
    const submit = () => handleSubmit(async (fields) => {
        if (busy.current || blocked || trackingLimit || paymentLimit)
            return;
        if (!original && !fields.catalogCategoryId) {
            setCatalogError(t("subscriptions:m_2ba638fe6b81"));
            return;
        }
        busy.current = true;
        setFailure(null);
        try {
            const body = toSubscriptionWrite(fields);
            const result = await mutation.mutateAsync(original ? { kind: 'update', id: original.id, rowVersion: original.rowVersion, body } : { kind: 'create', body });
            if (result) {
                const target = { pathname: '/subscription' as const, params: { id: result.id } };
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
                    if (error.status === 409 || error.kind === 'network' || error.kind === 'timeout')
                        setBlocked(true);
                    for (const name of Object.keys(subscriptionDefaults(today)) as (keyof SubscriptionFields)[]) {
                        const messages = error.errors?.[name] ?? error.errors?.[name[0].toUpperCase() + name.slice(1)];
                        if (messages?.length)
                            setError(name, { type: 'server', message: messages.join(' ') });
                    }
                }
            }
        }
        finally {
            busy.current = false;
        }
    })();
    const disabled = mutation.isPending || blocked;
    const input = (name: 'name' | 'providerName' | 'amount' | 'currencyCode' | 'billingInterval' | 'anchorDay' | 'description', label: string, maxLength?: number) => <Controller key={name} control={control} name={name} render={({ field }) => <AppInput label={label} value={field.value} onChangeText={field.onChange} onBlur={field.onBlur} error={errors[name]?.message} editable={!disabled} maxLength={maxLength} keyboardType={name === 'amount' ? 'decimal-pad' : ['billingInterval', 'anchorDay'].includes(name) ? 'number-pad' : 'default'} autoCapitalize={name === 'currencyCode' ? 'characters' : 'sentences'} multiline={name === 'description'}/>}/>;
    const date = (name: 'startDate' | 'firstBillingDate' | 'trialStartDate' | 'trialEndDate' | 'accessEndsOn', label: string, minimum?: string) => <Controller key={name} control={control} name={name} render={({ field }) => <DateField label={label} value={field.value} onChange={field.onChange} fallback={values.startDate || today} minimum={minimum} disabled={disabled} error={errors[name]?.message}/>}/>;
    const toggle = (name: 'autoRenew' | 'enableTrialReminders', label: string) => <Controller control={control} name={name} render={({ field }) => <View style={[styles.row, { gap: 12, minHeight: 56 }]}><AppText variant="label" style={{ flex: 1 }}>{label}</AppText><Switch accessibilityLabel={label} value={field.value} onValueChange={field.onChange} disabled={disabled}/></View>}/>;
    return <AppScreen keyboard><AppHeader title={original ? t("subscriptions:m_8810c95e97ad") : t("subscriptions:m_3d84aa9bcade")} subtitle={t("subscriptions:m_43b35e8a5c47")} back/><View style={{ gap: 20 }}>
    {entitlement ? <AppText variant="caption" tone="muted">{t("subscriptions:m_ad10ab3a2d2a", { value0: entitlement.plan, value1: trackingCount, value2: entitlement.limits.activeTrackedSubscriptions, value3: paymentCount, value4: entitlement.limits.activePaymentPlans })}</AppText> : <AppText variant="caption" tone="muted">{t("subscriptions:m_18527dae8cdd")}</AppText>}
    {(trackingLimit || paymentLimit || failure instanceof ApiError && failure.code === 'entitlement_limit') && <SubscriptionLimitNotice />}
    <CatalogSelectionField mode="Subscription" initialOpen={!original} categoryId={values.catalogCategoryId} serviceId={values.catalogServiceId} error={catalogError || errors.catalogCategoryId?.message || errors.catalogServiceId?.message} disabled={disabled} onSelect={selection => { setCatalogError(''); setValue('catalogCategoryId', selection.category.id); setValue('catalogServiceId', selection.service?.id ?? null); if (selection.service) {
        setValue('name', selection.service.displayName);
        setValue('providerName', selection.service.displayName.slice(0, 100));
        const f = selection.service.defaultRecurrenceType;
        if (f && f !== 'Once')
            setValue('billingFrequency', f);
    }
    else if (values.catalogServiceId) {
        setValue('name', '');
        setValue('providerName', '');
    } }}/>
    {!values.catalogServiceId || original ? <>{input('name', t("common:m_a45e9436a4ba"), 150)}{input('providerName', t("common:m_93f01c808be2"), 100)}</> : <AppText variant="section">{values.name}</AppText>}{input('amount', t("common:m_da20470ab885"))}{input('currencyCode', t("common:m_0c9e12052e29"), 3)}
<AppText variant="caption" tone="muted">{t("common:amountInputHint")}</AppText>
    <AppText variant="label">{t("subscriptions:m_bbc1a8b9becc")}</AppText><Controller control={control} name="billingFrequency" render={({ field }) => <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>{(Object.keys(billingLabels) as (keyof typeof billingLabels)[]).map(value => <View key={value} style={{ flexGrow: 1, minWidth: '45%' }}><AppButton title={billingLabels[value] + (field.value === value ? ' ✓' : '')} variant={field.value === value ? 'primary' : 'secondary'} disabled={disabled} onPress={() => { field.onChange(value); if (!['Monthly', 'Yearly'].includes(value))
        setValue('anchorDay', ''); }}/></View>)}</View>}/>
    {input('billingInterval', t("common:m_5ff71b7378f6"))}{date('startDate', t("subscriptions:m_39b03995fcce"))}{date('firstBillingDate', t("subscriptions:m_2140719bfbc7"), values.startDate)}
    <AppText variant="caption" tone="muted">{t("subscriptions:m_2ac439186d4f")}</AppText>
    {['Monthly', 'Yearly'].includes(frequency ?? '') && <>{input('anchorDay', t("common:m_62cae61c31f3"), 2)}<AppText variant="caption" tone="muted">{t("subscriptions:m_7754aee3cc38")}</AppText></>}
    <Controller control={control} name="status" render={({ field }) => <View style={{ gap: 8 }}>{(['Active', 'Trial'] as const).map(value => <AppButton key={value} title={(value === 'Active' ? t("subscriptions:m_0d95451acf39") : t("subscriptions:m_a75abe66fec2")) + (field.value === value ? ' ✓' : '')} variant={field.value === value ? 'primary' : 'secondary'} disabled={disabled || original?.status === 'Active' && value === 'Trial'} onPress={() => { field.onChange(value); if (value === 'Trial' && !values.trialStartDate)
        setValue('trialStartDate', values.startDate || today); }}/>)}</View>}/>
    {(status === 'Trial' || values.trialStartDate || values.trialEndDate) && <>{date('trialStartDate', t("subscriptions:m_38432ff279cf"), values.startDate)}{date('trialEndDate', t("subscriptions:m_ca1dff322c77"), values.trialStartDate || values.startDate)}{status === 'Active' && <AppButton title={t("subscriptions:m_046f79b76c42")} variant="ghost" disabled={disabled} onPress={() => { setValue('trialStartDate', ''); setValue('trialEndDate', ''); }}/>}{toggle('enableTrialReminders', t("subscriptions:m_6ace31f2854a"))}<AppText variant="caption" tone="muted">{t("subscriptions:m_075e106597b7")}</AppText></>}
    {toggle('autoRenew', t("subscriptions:m_c9f213296e2e"))}{date('accessEndsOn', t("subscriptions:m_6b853275a321"), values.startDate)}<AppButton title={t("subscriptions:m_e510182bc9ac")} variant="ghost" disabled={disabled || !values.accessEndsOn} onPress={() => setValue('accessEndsOn', '')}/>
    {input('description', t("common:m_9f4ce70fe7c2"), 2000)}
    {!!failure && <MutationFailure error={failure} blocked={blocked} reload={() => { void reload(); }}/>}
    <AppButton title={original ? t("subscriptions:m_cdaea12e4154") : t("subscriptions:m_9a75e128810d")} onPress={submit} loading={mutation.isPending} disabled={disabled || trackingLimit || paymentLimit}/>
    <AppText variant="caption" tone="muted">{t("subscriptions:m_cfcd181c31ea")}</AppText>
  </View></AppScreen>;
}
