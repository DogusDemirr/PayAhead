import { Pressable, View } from 'react-native';
import { FlaskConical, SlidersHorizontal } from 'lucide-react-native';
import { router, usePathname } from 'expo-router';
import { AppText, EmptyState, ErrorState, LoadingSkeleton, styles } from './ui';
import { useTheme } from '../theme/use-theme';
import { usePreview } from '../hooks/use-preview';
import { useUiStore } from '../store/ui';
import type { ReactNode } from 'react';

export function PreviewBanner() {
  const { colors } = useTheme(); const { enabled } = usePreview();
  const pathname = usePathname();
  if (!enabled || pathname !== '/preview') return null;
  return <Pressable accessibilityRole="button" accessibilityLabel="Tasarım önizlemesi, örnek veriler. Görünüm seçeneklerini aç" onPress={() => router.push('/preview')} style={[styles.row, { backgroundColor: colors.accentSoft, paddingHorizontal: 20, minHeight: 48, gap: 8 }]}><FlaskConical size={15} color={colors.accentText} /><AppText variant="caption" tone="accentText" style={{ flex: 1 }}>Tasarım önizlemesi · Örnek veriler</AppText><SlidersHorizontal color={colors.accentText} size={16} /></Pressable>;
}
export function PreviewContent({ children, emptyTitle, emptyMessage }: { children: ReactNode; emptyTitle?: string; emptyMessage?: string }) {
  const { state, enabled } = usePreview(); const setPreview = useUiStore((ui) => ui.setPreview);
  if (!enabled) return <EmptyState title="Finans görünümün hazırlanıyor" message="Hesap bağlantın hazır. Bu sürümde ödeme ve abonelik verileri henüz bağlı değil." />;
  if (state === 'loading') return <LoadingSkeleton />;
  if (state === 'error') return <ErrorState onRetry={() => setPreview('demo')} />;
  if (state === 'empty') return <EmptyState title={emptyTitle} message={emptyMessage} />;
  return <View>{children}</View>;
}
