import { t, useLocaleRevision, errorMessage, isRTL } from "../i18n/runtime";
import { useEffect, useState, type ReactNode } from 'react';
import { ActivityIndicator, KeyboardAvoidingView, Platform, Pressable, RefreshControl, ScrollView, StyleSheet, Text, TextInput, View, type TextProps, type TextInputProps, type ViewStyle, type StyleProp } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { ArrowUpRight, ChevronLeft, ChevronRight, Eye, EyeOff, Layers3, Plus, ShieldCheck, TriangleAlert, type LucideIcon } from 'lucide-react-native';
import Animated, { FadeInDown, useReducedMotion, useSharedValue, useAnimatedStyle, withRepeat, withTiming, cancelAnimation } from 'react-native-reanimated';
import { router } from 'expo-router';
import { useTheme } from '../theme/use-theme';
import { radius, space, typography, type Colors } from '../theme/tokens';
import { money } from '../utils/format';
export function AppText({ variant = 'body', tone = 'text', style, ...props }: TextProps & {
    variant?: keyof typeof typography;
    tone?: keyof Colors;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <Text {...props} style={[typography[variant], { color: colors[tone], writingDirection: isRTL() ? 'rtl' : 'ltr' }, style]}>{tone === 'danger' && typeof props.children === 'string' ? errorMessage(props.children, true) : props.children}</Text>;
}
export function AppScreen({ children, keyboard = false, refreshing = false, onRefresh }: {
    children: ReactNode;
    keyboard?: boolean;
    refreshing?: boolean;
    onRefresh?: () => void;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <SafeAreaView edges={['left', 'right', 'bottom']} style={{ flex: 1, backgroundColor: colors.background }}>
    <KeyboardAvoidingView style={{ flex: 1 }} behavior={keyboard && Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView refreshControl={onRefresh ? <RefreshControl refreshing={refreshing} onRefresh={onRefresh} tintColor={colors.accentText} colors={[colors.accentText]}/> : undefined} contentInsetAdjustmentBehavior="automatic" keyboardShouldPersistTaps="handled" contentContainerStyle={styles.screen} showsVerticalScrollIndicator={false}>{children}</ScrollView>
    </KeyboardAvoidingView>
  </SafeAreaView>;
}
export function AppCard({ children, style }: {
    children: ReactNode;
    style?: StyleProp<ViewStyle>;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <View style={[styles.card, { backgroundColor: colors.surface }, style]}>{children}</View>;
}
export function MotionCard({ children, delay = 0 }: {
    children: ReactNode;
    delay?: number;
}) {
    useLocaleRevision();
    const reduced = useReducedMotion();
    return <Animated.View entering={reduced ? undefined : FadeInDown.duration(260).delay(delay)}>{children}</Animated.View>;
}
export function AppButton({ title, onPress, variant = 'primary', icon: Icon, disabled = false, loading = false }: {
    title: string;
    onPress: () => void;
    variant?: 'primary' | 'secondary' | 'ghost' | 'destructive';
    icon?: LucideIcon;
    disabled?: boolean;
    loading?: boolean;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <Pressable accessibilityRole="button" accessibilityLabel={title} accessibilityState={{ disabled: disabled || loading, busy: loading }} onPress={onPress} disabled={disabled || loading} style={({ pressed }) => [styles.button, { backgroundColor: variant === 'destructive' ? colors.danger : variant === 'primary' ? colors.navy : variant === 'secondary' ? colors.accentSoft : 'transparent', opacity: disabled ? 0.45 : pressed ? 0.75 : 1, transform: [{ scale: pressed ? 0.985 : 1 }] }]}>
    {loading ? <ActivityIndicator color={colors.onNavy}/> : Icon ? <Icon size={19} color={variant === 'primary' || variant === 'destructive' ? colors.onNavy : colors.accentText}/> : null}
    <AppText variant="label" style={{ color: variant === 'primary' || variant === 'destructive' ? colors.onNavy : colors.accentText, textAlign: 'center', flexShrink: 1 }}>{title}</AppText>
  </Pressable>;
}
export function IconButton({ icon: Icon, label, onPress }: {
    icon: LucideIcon;
    label: string;
    onPress: () => void;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <Pressable accessibilityRole="button" accessibilityLabel={label} onPress={onPress} style={({ pressed }) => [styles.iconButton, { backgroundColor: colors.surface, opacity: pressed ? .6 : 1 }]}><Icon size={21} color={colors.text} strokeWidth={1.7}/></Pressable>;
}
export function AppHeader({ title, subtitle, right, back = false }: {
    title: string;
    subtitle?: string;
    right?: ReactNode;
    back?: boolean;
}) {
    useLocaleRevision();
    return <View style={[styles.row, { marginBottom: space.xxl, gap: space.md }]}>
    {back && <IconButton icon={isRTL() ? ChevronRight : ChevronLeft} label={t("common:m_b7e193af9550")} onPress={() => router.canGoBack() ? router.back() : router.replace('/')}/>}
    <View style={{ flex: 1 }}><AppText variant="title" accessibilityRole="header">{title}</AppText>{subtitle && <AppText tone="muted" style={{ marginTop: 4 }}>{subtitle}</AppText>}</View>{right}
  </View>;
}
export function AppInput({ label, error, secureTextEntry, style, ...props }: TextInputProps & {
    label: string;
    error?: string;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    error = errorMessage(error);
    const [visible, setVisible] = useState(false);
    const [focused, setFocused] = useState(false);
    return <View style={{ gap: space.sm }}>
    <AppText variant="label">{label}</AppText>
    <View style={[styles.inputWrap, { backgroundColor: colors.surface, borderColor: error ? colors.danger : focused ? colors.accentText : colors.border }]}>
      <TextInput {...props} accessibilityLabel={label} accessibilityHint={error} secureTextEntry={secureTextEntry && !visible} placeholderTextColor={colors.muted} onFocus={() => setFocused(true)} onBlur={(event) => { setFocused(false); props.onBlur?.(event); }} style={[typography.body, { color: colors.text, textAlign: isRTL() ? 'right' : 'left', writingDirection: isRTL() ? 'rtl' : 'ltr', flex: 1, paddingVertical: 15, minHeight: 54 }, style]}/>
      {secureTextEntry && <Pressable accessibilityRole="button" accessibilityLabel={visible ? t("common:m_7320279cca24") : t("common:m_8102f4f7ff35")} onPress={() => setVisible(!visible)} style={{ width: 48, height: 48, alignItems: 'center', justifyContent: 'center' }}>{visible ? <EyeOff color={colors.muted} size={20}/> : <Eye color={colors.muted} size={20}/>}</Pressable>}
    </View>
    {error && <AppText variant="caption" tone="danger" accessibilityRole="alert">{error}</AppText>}
  </View>;
}
export function CurrencyAmount({ amount, currency = 'TRY', large = false, inverse = false }: {
    amount: number;
    currency?: string;
    large?: boolean;
    inverse?: boolean;
}) {
    useLocaleRevision();
    return <AppText variant={large ? 'amount' : 'label'} tone={inverse ? 'onNavy' : 'text'} style={{ fontVariant: ['tabular-nums'], flexShrink: 1 }}>{money(amount, currency)}</AppText>;
}
export function StatusBadge({ label, tone = 'accent' }: {
    label: string;
    tone?: 'accent' | 'success' | 'warning' | 'muted';
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    const color = tone === 'accent' ? colors.accentText : colors[tone];
    const backgroundColor = tone === 'accent' ? colors.accentSoft : tone === 'success' ? colors.successSoft : tone === 'warning' ? colors.warningSoft : colors.elevated;
    return <View style={{ alignSelf: 'flex-start', paddingHorizontal: 9, paddingVertical: 4, borderRadius: radius.pill, backgroundColor }}><AppText variant="caption" style={{ color }}>{label}</AppText></View>;
}
export function SectionHeader({ title, action, onPress }: {
    title: string;
    action?: string;
    onPress?: () => void;
}) {
    useLocaleRevision();
    return <View style={[styles.row, { marginTop: space.xxl, marginBottom: space.md }]}><AppText variant="section" accessibilityRole="header" style={{ flex: 1 }}>{title}</AppText>{action && <Pressable accessibilityRole="button" onPress={onPress} style={{ minHeight: 48, paddingLeft: 12, justifyContent: 'center' }}><AppText variant="caption" tone="accentText">{action} {isRTL() ? '←' : '→'}</AppText></Pressable>}</View>;
}
export function Brand({ large = false }: {
    large?: boolean;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <View style={[styles.row, { justifyContent: 'flex-start', gap: 9 }]}><View style={{ width: large ? 46 : 34, height: large ? 46 : 34, borderRadius: 12, backgroundColor: colors.accentSoft, alignItems: 'center', justifyContent: 'center' }}><ArrowUpRight color={colors.accentText} size={large ? 30 : 24} strokeWidth={2.6}/></View><AppText variant={large ? 'title' : 'section'} style={{ letterSpacing: -0.8 }}>PayAhead<AppText variant="caption" tone="accentText"> •</AppText></AppText></View>;
}
export function EmptyState({ title = t("common:m_2eaf24d56ba0"), message = t("common:m_764562096839"), action, onPress }: {
    title?: string;
    message?: string;
    action?: string;
    onPress?: () => void;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <View style={styles.state}><View style={[styles.stateIcon, { backgroundColor: colors.accentSoft }]}><Layers3 color={colors.accentText} size={30}/></View><AppText variant="section" style={{ textAlign: 'center' }}>{title}</AppText><AppText tone="muted" style={{ textAlign: 'center', maxWidth: 280 }}>{message}</AppText>{action && onPress && <AppButton title={action} onPress={onPress} icon={Plus}/>}</View>;
}
export function ErrorState({ onRetry }: {
    onRetry: () => void;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <View style={styles.state} accessibilityLiveRegion="polite"><TriangleAlert color={colors.warning} size={32}/><AppText variant="section">{t("common:m_ef8c222befc5")}</AppText><AppText tone="muted" style={{ textAlign: 'center' }}>{t("common:m_4c97e807cb38")}</AppText><AppButton title={t("common:m_0ce9380b07f8")} onPress={onRetry} variant="secondary"/></View>;
}
export function LoadingSkeleton() {
    useLocaleRevision();
    const { colors } = useTheme();
    const reduced = useReducedMotion();
    const opacity = useSharedValue(1);
    useEffect(() => { if (!reduced)
        opacity.value = withRepeat(withTiming(.55, { duration: 850 }), -1, true); return () => cancelAnimation(opacity); }, [reduced, opacity]);
    const animatedStyle = useAnimatedStyle(() => ({ opacity: opacity.value }));
    return <Animated.View accessible accessibilityLabel={t("common:m_317a7648ba7f")} accessibilityRole="progressbar" style={[{ gap: 14 }, animatedStyle]}>{[180, 76, 76].map((height, index) => <View key={index} style={{ height, borderRadius: radius.card, backgroundColor: colors.elevated, overflow: 'hidden', padding: 20 }}><View style={{ height: 12, width: '45%', borderRadius: 8, backgroundColor: colors.border }}/><View style={{ height: 20, width: '70%', marginTop: 18, borderRadius: 8, backgroundColor: colors.border }}/></View>)}</Animated.View>;
}
export function Assurance({ text = t("common:m_059faeaf0f7c") }: {
    text?: string;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <View style={[styles.row, { justifyContent: 'center', gap: 6, marginVertical: 20 }]}><ShieldCheck color={colors.muted} size={15}/><AppText variant="caption" tone="muted">{text}</AppText></View>;
}
export function SettingRow({ title, subtitle, icon: Icon, onPress, value }: {
    title: string;
    subtitle?: string;
    icon: LucideIcon;
    onPress: () => void;
    value?: string;
}) {
    useLocaleRevision();
    const { colors } = useTheme();
    return <Pressable accessibilityRole="button" accessibilityLabel={t("common:m_aa81c136b41c", { value0: title, value1: value ? `, ${value}` : '' })} onPress={onPress} style={({ pressed }) => [styles.row, { minHeight: 68, gap: 12, paddingVertical: 10, opacity: pressed ? .6 : 1 }]}><Icon color={colors.muted} size={21} strokeWidth={1.7}/><View style={{ flex: 1 }}><AppText variant="label">{title}</AppText>{subtitle && <AppText variant="caption" tone="muted">{subtitle}</AppText>}</View>{value && <AppText variant="caption" tone="muted">{value}</AppText>}<ChevronRight color={colors.muted} size={17} style={{ transform: [{ scaleX: isRTL() ? -1 : 1 }] }}/></Pressable>;
}
export const styles = StyleSheet.create({
    screen: { paddingHorizontal: space.xl, paddingTop: space.lg, paddingBottom: space.xxxl },
    card: { borderRadius: radius.card, padding: space.xl },
    row: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
    button: { minHeight: 54, borderRadius: radius.button, paddingHorizontal: 20, paddingVertical: 14, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8 },
    iconButton: { width: 48, height: 48, borderRadius: 16, alignItems: 'center', justifyContent: 'center' },
    inputWrap: { borderWidth: 1, borderRadius: radius.input, paddingLeft: 16, flexDirection: 'row', alignItems: 'center', paddingRight: 4 },
    state: { paddingVertical: 40, alignItems: 'center', gap: 14 },
    stateIcon: { width: 68, height: 68, borderRadius: 24, alignItems: 'center', justifyContent: 'center' },
});
