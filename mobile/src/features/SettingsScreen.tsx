import { ProfileAvatar } from './account/avatar';
import { useState } from 'react';
import { View, Pressable } from 'react-native';
import { router } from 'expo-router';
import { Bell, ChevronRight, CircleUserRound, Crown, LogOut, Moon, ShieldCheck, Sun, Smartphone } from 'lucide-react-native';
import { AppScreen, AppHeader, AppCard, AppText, SectionHeader, SettingRow, StatusBadge, styles } from '../components/ui';
import { useTheme } from '../theme/use-theme';
import { useUiStore, type ThemePreference } from '../store/ui';
import { useCurrentUser } from '../hooks/use-current-user';
import { session } from '../services/auth-runtime';
import { t, useLocaleRevision } from '../i18n/runtime';
export default function SettingsScreen() {
    useLocaleRevision();
    const { colors } = useTheme();
    const theme = useUiStore((ui) => ui.theme);
    const setTheme = useUiStore((ui) => ui.setTheme);
    const { data: user, isError, refetch } = useCurrentUser();
    const [loggingOut, setLoggingOut] = useState(false);
    return <AppScreen><AppHeader title={t("settings:m_80ad54ad05fa")} subtitle={user ? t("settings:m_64bd1224db11") : t("settings:m_9b7337a7225a")}/>
    <AppCard><View style={[styles.row, { justifyContent: 'flex-start', gap: 14 }]}><ProfileAvatar id={user?.id ?? ''} name={user?.displayName ?? ''} hasAvatar={user?.hasAvatar} version={user?.avatarVersion} size={56} /><View style={{ flex: 1 }}><AppText variant="section">{user?.displayName ?? t("settings:m_a20eda1e3a04")}</AppText><AppText variant="caption" tone="muted" style={{ marginTop: 4 }}>{user?.email ?? t("settings:m_121128a3449b")}</AppText></View><StatusBadge label={t("settings:m_1c56ac8f2dc6")} tone="muted"/></View></AppCard>
    <>{isError && <Pressable accessibilityRole="button" onPress={() => { void refetch(); }} style={{ minHeight: 48, justifyContent: 'center' }}><AppText tone="danger">{t("settings:m_69022fb48e27")}</AppText></Pressable>}</><SectionHeader title={t("settings:m_e1b0af0c144d")}/><AppCard><View style={[styles.row, { marginBottom: 18, justifyContent: 'flex-start', gap: 10 }]}><Moon size={19} color={colors.muted}/><AppText variant="label">{t("settings:m_7c08e78575d6")}</AppText></View><View style={[styles.row, { gap: 8 }]}>{[{ title: t("settings:m_bbb1132f30f1"), value: 'light', icon: Sun }, { title: t("settings:m_b24a36089466"), value: 'dark', icon: Moon }, { title: t("settings:m_1c466db79243"), value: 'system', icon: Smartphone }].map(({ title, value, icon: Icon }) => <Pressable key={value} accessibilityRole="radio" accessibilityState={{ checked: theme === value }} onPress={() => setTheme(value as ThemePreference)} style={{ flex: 1, minHeight: 72, borderRadius: 14, alignItems: 'center', justifyContent: 'center', gap: 8, backgroundColor: theme === value ? colors.accentSoft : colors.elevated }}><Icon color={theme === value ? colors.accentText : colors.muted} size={19}/><AppText variant="caption" tone={theme === value ? 'accentText' : 'muted'}>{title}</AppText></Pressable>)}</View></AppCard>
    <SectionHeader title={t("settings:m_366fcd93889e")}/><AppCard><SettingRow title={t('settings:languageTitle')} icon={Smartphone} onPress={() => router.push('/language')}/><SettingRow title={t("settings:m_be761e877712")} icon={Bell} onPress={() => router.push('/notifications')}/></AppCard>
    <SectionHeader title={t("settings:m_1c56ac8f2dc6")}/><AppCard style={{ paddingVertical: 4 }}><SettingRow title={t("settings:m_a13a55a714f1")} icon={CircleUserRound} onPress={() => router.push('/account')}/><SettingRow title={t("settings:m_6f71eadb6aaa")} icon={ShieldCheck} onPress={() => router.push('/privacy-security')}/></AppCard>
    <Pressable accessibilityRole="button" onPress={() => router.push('/premium')} style={{ marginTop: 24 }}><AppCard style={{ backgroundColor: colors.navy }}><View style={[styles.row, { gap: 12 }]}><Crown color={colors.accent} size={26}/><View style={{ flex: 1 }}><AppText variant="section" tone="onNavy">{t("settings:m_21858317874a")}</AppText><AppText variant="caption" tone="onNavyMuted" style={{ marginTop: 5 }}>{t("settings:m_f43b35270139")}</AppText></View><ChevronRight color={colors.onNavyMuted} size={20}/></View></AppCard></Pressable>
    <Pressable accessibilityRole="button" disabled={loggingOut} accessibilityState={{ busy: loggingOut, disabled: loggingOut }} onPress={() => { setLoggingOut(true); void session.logout().finally(() => setLoggingOut(false)); }} style={[styles.row, { justifyContent: 'center', minHeight: 60, marginTop: 20, gap: 10 }]}><LogOut color={colors.muted} size={18}/><AppText variant="label" tone="muted">{loggingOut ? t("settings:m_69fbb458f5f0") : t("settings:m_ea5b69c1ad74")}</AppText></Pressable><AppText variant="caption" tone="muted" style={{ textAlign: 'center', marginTop: 4 }}>{t("settings:m_f7b305154a1e")}</AppText>
  </AppScreen>;
}
