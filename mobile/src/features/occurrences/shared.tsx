import { currentLocale, t, useLocaleRevision } from '../../i18n/runtime';
import { Pressable, View } from 'react-native';
import { router } from 'expo-router';
import { AppText, StatusBadge } from '../../components/ui';
import { FinancialAmount } from '../payments/components';
import { dateLabel } from '../../utils/format';
import type { Projected, OccurrenceStatus } from '../payments/contracts';
import { CatalogIdentity } from '../catalog/CatalogIdentity';
export function statusLabel(status: OccurrenceStatus, date: string, today: string) { return status === 'Paid' ? t("calendar:m_2f36f3362ead") : status === 'Skipped' ? t("calendar:m_594b1254c47d") : date < today ? t("calendar:m_bffd1dfbfbed") : t("calendar:m_9025b4af025a"); }
export function OccurrenceRow({ item, today }: {
    item: Projected;
    today: string;
}) {
    useLocaleRevision();
    return <Pressable accessibilityRole="button" accessibilityLabel={t("calendar:m_66f71e58bc91", { value0: item.paymentName, value1: dateLabel(item.dueDate), value2: statusLabel(item.status, item.dueDate, today) })} onPress={() => router.push({ pathname: '/occurrence', params: { id: item.paymentPlanId, date: item.dueDate } })} style={{ minHeight: 96, paddingVertical: 14, gap: 8 }}>
    <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}><View style={{ flex: 1 }}><AppText variant="label">{item.paymentName}</AppText><AppText variant="caption" tone="muted">{t("calendar:m_86276a545c73", { value0: dateLabel(item.dueDate, false, true) })}</AppText></View><FinancialAmount amount={item.amount} currency={item.currencyCode}/></View>
    <StatusBadge label={statusLabel(item.status, item.dueDate, today)} tone={item.status === 'Paid' ? 'success' : item.status === 'Skipped' ? 'muted' : item.dueDate < today ? 'warning' : 'accent'}/>
    <CatalogIdentity categoryId={item.catalogCategoryId} serviceId={item.catalogServiceId} fallback={item.category} compact/>
    {item.status === 'Paid' && <AppText variant="caption" tone="muted">{t("calendar:m_4a7458f9b406", { value0: item.paidAtUtc ? new Date(item.paidAtUtc).toLocaleString(currentLocale()) : t("calendar:m_3a56ed96d493") })}</AppText>}
  </Pressable>;
}
