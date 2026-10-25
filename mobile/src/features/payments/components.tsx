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
export function FinancialAmount({ amount, currency, large = false, inverse = false }: { amount: string; currency: string; large?: boolean; inverse?: boolean }) {
  return <AppText variant={large ? 'amount' : 'label'} tone={inverse ? 'onNavy' : 'text'} style={{ fontVariant: ['tabular-nums'], flexShrink: 1 }}>{financialMoney(amount, currency)}</AppText>;
}
export function FinanceError({ error, retry }: { error: unknown; retry: () => void }) {
  return <AppCard><AppText variant="section">Şu an gösteremiyoruz</AppText><AppText tone="muted" accessibilityRole="alert" style={{ marginVertical: 12 }}>{error instanceof ApiError ? error.message : 'Bağlantını kontrol edip tekrar deneyebilirsin.'}</AppText><AppButton title="Tekrar dene" variant="secondary" onPress={retry} /></AppCard>;
}
export function LimitNotice({ message }: { message?: string }) {
  const { colors } = useTheme();
  return <AppCard style={{ backgroundColor: colors.warningSoft, marginVertical: 16 }}><AppText variant="section">Planlarına biraz daha alan</AppText><AppText tone="warning" style={{ marginVertical: 12 }}>{message ?? 'Aktif ödeme planı limitine ulaştın. Mevcut bir planı pasifleştirebilir veya Premium seçeneklerini inceleyebilirsin.'}</AppText><AppButton title="Premium’u incele" variant="secondary" onPress={() => router.push('/premium')} /></AppCard>;
}
export function PlanRow({ plan, next }: { plan: PaymentPlan; next?: string | null }) {
  const { colors } = useTheme();
  return <Pressable accessibilityRole="button" accessibilityLabel={`${plan.name} detayını aç`} onPress={() => router.push({ pathname: '/payment', params: { id: plan.id } })} style={({ pressed }) => ({ opacity: pressed ? .65 : 1, borderBottomColor: colors.border, borderBottomWidth: .5, paddingVertical: 18, gap: 12 })}>
    <View style={[styles.row, { gap: 12, alignItems: 'flex-start' }]}><WalletCards color={colors.accentText} size={24} /><View style={{ flex: 1 }}><AppText variant="label">{plan.name}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 5 }}>{frequencyLabels[plan.frequency]} · Her {plan.interval} dönem</AppText></View><ArrowRight size={18} color={colors.muted} /></View>
    <View style={[styles.row, { flexWrap: 'wrap', gap: 10 }]}><FinancialAmount amount={plan.amount} currency={plan.currencyCode} /><StatusBadge label={plan.isActive ? 'Aktif plan' : 'Pasif plan'} tone={plan.isActive ? 'accent' : 'muted'} /></View>
    <CatalogIdentity categoryId={plan.catalogCategoryId} serviceId={plan.catalogServiceId} fallback={plan.category} />
    <AppText variant="caption" tone="muted">{next === undefined ? 'Plan tarihi yükleniyor…' : next ? `Planlanan: ${dateLabel(next, true, true)}` : 'Yeni plan tarihi yok'}</AppText>
  </Pressable>;
}
export function ProjectedRow({ item, overdue = false }: { item: Projected; overdue?: boolean }) {
  const { colors } = useTheme();
  return <Pressable accessibilityRole="button" accessibilityLabel={`${item.paymentName}, ${overdue ? 'geciken' : 'yaklaşan'} ödeme`} onPress={() => router.push({ pathname: '/occurrence', params: { id: item.paymentPlanId, date: item.dueDate } })} style={{ minHeight: 90, paddingVertical: 14, gap: 8, borderBottomWidth: .5, borderBottomColor: colors.border }}>
    <AppText variant="label">{item.paymentName}</AppText><CatalogIdentity categoryId={item.catalogCategoryId} serviceId={item.catalogServiceId} fallback={item.category} compact /><View style={[styles.row, { flexWrap: 'wrap', gap: 8 }]}><AppText variant="caption" tone={overdue ? 'warning' : 'muted'}>{dateLabel(item.dueDate, true, true)} · {overdue ? 'Gecikti' : 'Bekliyor'}</AppText><FinancialAmount amount={item.amount} currency={item.currencyCode} /></View>
  </Pressable>;
}
export function MonthlySummary({ summary }: { summary: Summary }) {
  const { colors } = useTheme();
  return <LinearGradient colors={[colors.navy, colors.decoration]} style={{ borderRadius: 24, padding: 24, overflow: 'hidden' }}>
    <View pointerEvents="none" style={{ position: 'absolute', width: 190, height: 190, borderRadius: 100, borderWidth: 1, borderColor: colors.onNavyMuted, opacity: .12, right: -90, top: -90 }} />
    <AppText variant="caption" tone="onNavyMuted" style={{ letterSpacing: 1.4 }}>BU AY · YAKLAŞAN ÖDEMELER</AppText>
    {summary.upcomingTotalsByCurrency.length ? summary.upcomingTotalsByCurrency.map((total) => <View key={total.currencyCode} style={{ marginTop: 18 }}><FinancialAmount amount={total.amount} currency={total.currencyCode} large inverse /><AppText variant="caption" tone="onNavyMuted">{total.currencyCode}</AppText></View>) : <AppText variant="section" tone="onNavy" style={{ marginTop: 20 }}>Yaklaşan tutarın yok.</AppText>}
    <AppText variant="caption" tone="onNavyMuted" style={{ marginTop: 20 }}>{summary.upcomingPaymentCount} yaklaşan · {summary.activePaymentPlanCount} aktif plan</AppText>
    <AppText variant="caption" tone="onNavyMuted" style={{ marginTop: 8 }}>Bugünden ay sonuna kadar. Para birimleri ayrı gösterilir.</AppText>
  </LinearGradient>;
}
