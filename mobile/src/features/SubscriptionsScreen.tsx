import { useState } from 'react';
import { View, Pressable } from 'react-native';
import { Plus } from 'lucide-react-native';
import { router } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, CurrencyAmount, IconButton, SectionHeader, styles } from '../components/ui';
import { SubscriptionCard } from '../components/finance';
import { PreviewContent } from '../components/preview';
import { previewSubscriptions } from './preview-data';
import { currencyTotals } from '../utils/format';
import { useTheme } from '../theme/use-theme';

export default function SubscriptionsScreen() {
  const { colors } = useTheme(); const [trials, setTrials] = useState(false);
  return <AppScreen><AppHeader title="Abonelikler" subtitle="Küçük tutarlar, büyük bir netlik." right={<IconButton icon={Plus} label="Abonelik ekleme bilgisi" onPress={() => router.push({ pathname: '/information', params: { title: 'Yeni abonelik', message: 'Abonelik oluşturma bu sürümde henüz kullanılamıyor. Önizlemede mevcut kartları inceleyebilirsin.' } })} />} />
    <PreviewContent emptyTitle="Aboneliklerini tek yerde topla" emptyMessage="Aktif aboneliklerini ve ücretsiz denemelerini burada takip edebileceksin.">
      <AppCard><AppText variant="caption" tone="muted" style={{ letterSpacing: 1 }}>AYLIK TAHMİNİ KARŞILIK</AppText>{currencyTotals(previewSubscriptions.filter((item) => !item.trialDays)).map((total) => <View key={total.currencyCode} style={[styles.row, { marginTop: 14 }]}><CurrencyAmount amount={total.amount} currency={total.currencyCode} large /><AppText variant="caption" tone="muted">{total.currencyCode} / ay</AppText></View>)}<AppText variant="caption" tone="muted" style={{ marginTop: 16 }}>Para birimleri ayrı gösterilir. Ücretsiz denemeler dahil değil.</AppText></AppCard>
      <View style={[styles.row, { gap: 8, marginTop: 24, marginBottom: 8 }]}>{[{ title: 'Aktif abonelikler', selected: !trials }, { title: 'Ücretsiz denemeler', selected: trials }].map((tab, index) => <Pressable key={tab.title} accessibilityRole="tab" accessibilityState={{ selected: tab.selected }} onPress={() => setTrials(index === 1)} style={{ flex: 1, minHeight: 48, alignItems: 'center', justifyContent: 'center', borderRadius: 14, padding: 10, backgroundColor: tab.selected ? colors.accentSoft : colors.surface }}><AppText variant="caption" tone={tab.selected ? 'accentText' : 'muted'}>{tab.title}</AppText></Pressable>)}</View>
      <SectionHeader title={trials ? 'Denemen bitmeden' : 'Yaklaşan yenilemeler'} />{previewSubscriptions.filter((item) => !!item.trialDays === trials).map((item) => <SubscriptionCard key={item.id} item={item} onPress={() => router.push({ pathname: '/information', params: { title: item.name, message: `${item.provider}. Bu bir tasarım önizlemesidir; yenileme veya iptal işlemi yapılmaz.` } })} />)}
    </PreviewContent>
  </AppScreen>;
}
