import { Pressable, View } from 'react-native';
import { router } from 'expo-router';
import { AppText, StatusBadge } from '../../components/ui';
import { FinancialAmount } from '../payments/components';
import { dateLabel } from '../../utils/format';
import type { Projected, OccurrenceStatus } from '../payments/contracts';
export function statusLabel(status: OccurrenceStatus, date: string, today: string) { return status === 'Paid' ? 'Ödendi' : status === 'Skipped' ? 'Atlandı' : date < today ? 'Gecikti' : 'Bekliyor'; }
export function OccurrenceRow({ item, today }: { item: Projected; today: string }) {
  return <Pressable accessibilityRole="button" accessibilityLabel={`${item.paymentName}, ${dateLabel(item.dueDate)}, ${statusLabel(item.status, item.dueDate, today)}`} onPress={() => router.push({ pathname: '/occurrence', params: { id: item.paymentPlanId, date: item.dueDate } })} style={{ minHeight: 96, paddingVertical: 14, gap: 8 }}>
    <View style={{ flexDirection: 'row', gap: 10, alignItems: 'flex-start' }}><View style={{ flex: 1 }}><AppText variant="label">{item.paymentName}</AppText><AppText variant="caption" tone="muted">Planlanan: {dateLabel(item.dueDate, false, true)}</AppText></View><FinancialAmount amount={item.amount} currency={item.currencyCode} /></View>
    <StatusBadge label={statusLabel(item.status, item.dueDate, today)} tone={item.status === 'Paid' ? 'success' : item.status === 'Skipped' ? 'muted' : item.dueDate < today ? 'warning' : 'accent'} />
    {item.status === 'Paid' && <AppText variant="caption" tone="muted">Gerçek ödenen tutar · {item.paidAtUtc ? new Date(item.paidAtUtc).toLocaleString('tr-TR') : 'Ödeme zamanı bulunamadı'}</AppText>}
  </Pressable>;
}
