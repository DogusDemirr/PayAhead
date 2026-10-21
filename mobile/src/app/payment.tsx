import { useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, CurrencyAmount, StatusBadge, EmptyState, styles } from '../components/ui';
import { View } from 'react-native';
import { CategoryIcon } from '../components/finance';
import { previewPayments } from '../features/preview-data';
import { usePreview } from '../hooks/use-preview';
import { dateLabel } from '../utils/format';
export default function Payment() { const { id } = useLocalSearchParams<{ id: string }>(); const { state } = usePreview(); const item = state === 'demo' ? previewPayments.find((p) => p.id === id) : undefined; return <AppScreen><AppHeader title="Ödeme detayı" back />{item ? <AppCard><CategoryIcon category={item.category} /><AppText variant="title" style={{ marginTop: 20 }}>{item.name}</AppText><View style={{ marginVertical: 20 }}><CurrencyAmount amount={item.amount} currency={item.currencyCode} large /></View><View style={[styles.row, { marginBottom: 16 }]}><AppText tone="muted">Planlanan tarih</AppText><AppText variant="label">{dateLabel(item.date, true)}</AppText></View><StatusBadge label={item.status === 'Paid' ? 'Ödendi' : item.status === 'Skipped' ? 'Atlandı' : 'Yaklaşıyor'} /><AppText variant="caption" tone="muted" style={{ marginTop: 24 }}>Bu bir örnek ödeme. Tahsilat veya durum değişikliği yapılmaz.</AppText></AppCard> : <EmptyState title="Gösterilecek ödeme yok" />}</AppScreen>; }
