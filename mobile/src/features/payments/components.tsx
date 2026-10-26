import { t, useLocaleRevision, errorMessage, isRTL } from "../../i18n/runtime";
import { Pressable, View } from 'react-native';
import { router } from 'expo-router';
import { WalletCards, ArrowRight } from 'lucide-react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { AppButton, AppCard, AppText, StatusBadge, styles } from '../../components/ui';
import { useTheme } from '../../theme/use-theme';
import { ApiError } from '../../services/api-error';
import { financialMoney } from './money';
import { frequencyLabels } from './validation';
import type { PaymentPlan, Projected, Summary } from './contracts';
import { dateLabel } from '../../utils/format';
import { CatalogIdentity } from '../catalog/CatalogIdentity';
export function FinancialAmount({ amount, currency, large = false, inverse = false }: {
    amount: string;
    currency: string;
    large?: boolean;
    inverse?: boolean;
}) {
    useLocaleRevision();
    return <AppText variant={large ? 'amount' : 'label'} tone={inverse ? 'onNavy' : 'text'} style={{ fontVariant: ['tabular-nums'], flexShrink: 1 }}>{financialMoney(amount, currency)}</AppText>;
}
export function FinanceError({ error, retry }: {
    error: unknown;
    retry: () => void;
}) {
    useLocaleRevision();
    return <AppCard><AppText variant="section">{t("payments:m_ef8c222befc5")}</AppText><AppText tone="muted" accessibilityRole="alert" style={{ marginVertical: 12 }}>{error instanceof ApiError ? errorMessage(error.message, true) : t("payments:m_9b81103a9b48")}</AppText><AppButton title={t("payments:m_0ce9380b07f8")} variant="secondary" onPress={retry}/></AppCard>;
}
export function LimitNotice({ message }: {
    message?: string;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <AppCard style={{ backgroundColor: colors.warningSoft, marginVertical: 16 }}><AppText variant="section">{t("payments:m_687f69a3496a")}</AppText><AppText tone="warning" style={{ marginVertical: 12 }}>{message ?? t("payments:m_287ade462d7f")}</AppText><AppButton title={t("payments:m_875c469eccea")} variant="secondary" onPress={() => router.push('/premium')}/></AppCard>;
}
export function PlanRow({ plan, next }: {
    plan: PaymentPlan;
    next?: string | null;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <Pressable accessibilityRole="button" accessibilityLabel={t("payments:m_0d3206e39f9e", { value0: plan.name })} onPress={() => router.push({ pathname: '/payment', params: { id: plan.id } })} style={({ pressed }) => ({ opacity: pressed ? .65 : 1, borderBottomColor: colors.border, borderBottomWidth: .5, paddingVertical: 18, gap: 12 })}>
    <View style={[styles.row, { gap: 12, alignItems: 'flex-start' }]}><WalletCards color={colors.accentText} size={24}/><View style={{ flex: 1 }}><AppText variant="label">{plan.name}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 5 }}>{t("payments:m_82413ba35131", { value0: frequencyLabels[plan.frequency], value1: plan.interval })}</AppText></View><ArrowRight style={{ transform: [{ scaleX: isRTL() ? -1 : 1 }] }} size={18} color={colors.muted}/></View>
    <View style={[styles.row, { flexWrap: 'wrap', gap: 10 }]}><FinancialAmount amount={plan.amount} currency={plan.currencyCode}/><StatusBadge label={plan.isActive ? t("payments:m_b31385e605a6") : t("payments:m_aff1d602e8dd")} tone={plan.isActive ? 'accent' : 'muted'}/></View>
    <CatalogIdentity categoryId={plan.catalogCategoryId} serviceId={plan.catalogServiceId} fallback={plan.category}/>
    <AppText variant="caption" tone="muted">{next === undefined ? t("payments:m_97b31ba37783") : next ? t("calendar:m_86276a545c73", { value0: dateLabel(next, true, true) }) : t("payments:m_5c0fc0b491b9")}</AppText>
  </Pressable>;
}
export function ProjectedRow({ item, overdue = false }: {
    item: Projected;
    overdue?: boolean;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <Pressable accessibilityRole="button" accessibilityLabel={t("payments:m_3dfd364d95c2", { value0: item.paymentName, value1: overdue ? t("payments:m_bffd1dfbfbed") : t("payments:m_d19d95f2f5f1") })} onPress={() => router.push({ pathname: '/occurrence', params: { id: item.paymentPlanId, date: item.dueDate } })} style={{ minHeight: 90, paddingVertical: 14, gap: 8, borderBottomWidth: .5, borderBottomColor: colors.border }}>
    <AppText variant="label">{item.paymentName}</AppText><CatalogIdentity categoryId={item.catalogCategoryId} serviceId={item.catalogServiceId} fallback={item.category} compact/><View style={[styles.row, { flexWrap: 'wrap', gap: 8 }]}><AppText variant="caption" tone={overdue ? 'warning' : 'muted'}>{dateLabel(item.dueDate, true, true)} · {overdue ? t("payments:m_bffd1dfbfbed") : t("payments:m_9025b4af025a")}</AppText><FinancialAmount amount={item.amount} currency={item.currencyCode}/></View>
  </Pressable>;
}
export function MonthlySummary({ summary }: {
    summary: Summary;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <LinearGradient accessibilityHint={t("common:paymentCount", { count: summary.upcomingPaymentCount })} colors={[colors.navy, colors.decoration]} style={{ borderRadius: 24, padding: 24, overflow: 'hidden' }}>
    <View pointerEvents="none" style={{ position: 'absolute', width: 190, height: 190, borderRadius: 100, borderWidth: 1, borderColor: colors.onNavyMuted, opacity: .12, right: -90, top: -90 }}/>
    <AppText variant="caption" tone="onNavyMuted" style={{ letterSpacing: 1.4 }}>{t("payments:m_60c98fa1f6ed")}</AppText>
    {summary.upcomingTotalsByCurrency.length ? summary.upcomingTotalsByCurrency.map((total) => <View key={total.currencyCode} style={{ marginTop: 18 }}><FinancialAmount amount={total.amount} currency={total.currencyCode} large inverse/><AppText variant="caption" tone="onNavyMuted">{total.currencyCode}</AppText></View>) : <AppText variant="section" tone="onNavy" style={{ marginTop: 20 }}>{t("payments:m_e12baefecd33")}</AppText>}
    <AppText variant="caption" tone="onNavyMuted" style={{ marginTop: 20 }}>{t("payments:m_887b8091622e", { value0: summary.upcomingPaymentCount, value1: summary.activePaymentPlanCount })}</AppText>
    <AppText variant="caption" tone="onNavyMuted" style={{ marginTop: 8 }}>{t("payments:m_7d5afa5c041e")}</AppText>
  </LinearGradient>;
}
