import { t, useLocaleRevision } from "../i18n/runtime";
import { Pressable, View } from 'react-native';
import { FlaskConical, SlidersHorizontal } from 'lucide-react-native';
import { router, usePathname } from 'expo-router';
import { AppText, EmptyState, ErrorState, LoadingSkeleton, styles } from './ui';
import { useTheme } from '../theme/use-theme';
import { usePreview } from '../hooks/use-preview';
import { useUiStore } from '../store/ui';
import type { ReactNode } from 'react';
export function PreviewBanner() {
    useLocaleRevision();
    const { colors } = useTheme();
    const { enabled } = usePreview();
    const pathname = usePathname();
    if (!enabled || pathname !== '/preview')
        return null;
    return <Pressable accessibilityRole="button" accessibilityLabel={t("common:m_f9b5f266396e")} onPress={() => router.push('/preview')} style={[styles.row, { backgroundColor: colors.accentSoft, paddingHorizontal: 20, minHeight: 48, gap: 8 }]}><FlaskConical size={15} color={colors.accentText}/><AppText variant="caption" tone="accentText" style={{ flex: 1 }}>{t("common:m_87b958b6436a")}</AppText><SlidersHorizontal color={colors.accentText} size={16}/></Pressable>;
}
export function PreviewContent({ children, emptyTitle, emptyMessage }: {
    children: ReactNode;
    emptyTitle?: string;
    emptyMessage?: string;
}) {
    useLocaleRevision();
    const { state, enabled } = usePreview();
    const setPreview = useUiStore((ui) => ui.setPreview);
    if (!enabled)
        return <EmptyState title={t("common:m_2d0d3fe32985")} message={t("common:m_87a4bbff702f")}/>;
    if (state === 'loading')
        return <LoadingSkeleton />;
    if (state === 'error')
        return <ErrorState onRetry={() => setPreview('demo')}/>;
    if (state === 'empty')
        return <EmptyState title={emptyTitle} message={emptyMessage}/>;
    return <View>{children}</View>;
}
