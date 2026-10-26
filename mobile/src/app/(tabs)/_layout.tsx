import { t, useLocaleRevision } from "../../i18n/runtime";
import { Tabs } from 'expo-router/js-tabs';
import { Redirect } from 'expo-router';
import { House, WalletCards, CalendarDays, Repeat2, Settings2 } from 'lucide-react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTheme } from '../../theme/use-theme';
import { useSessionStore } from '../../store/session';
import { useReducedMotion } from 'react-native-reanimated';
export default function TabsLayout() {
    useLocaleRevision();
    const { colors } = useTheme();
    const status = useSessionStore((state) => state.status);
    const insets = useSafeAreaInsets();
    const reduced = useReducedMotion();
    if (status !== 'authenticated')
        return <Redirect href="/(auth)/login"/>;
    return <Tabs backBehavior="history" screenOptions={{ headerShown: false, animation: reduced ? 'none' : 'fade', tabBarActiveTintColor: colors.accentText, tabBarInactiveTintColor: colors.muted, tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border, height: 72 + insets.bottom, paddingBottom: Math.max(insets.bottom, 8), paddingTop: 12 }, tabBarLabelStyle: { fontFamily: 'Inter_500Medium', fontSize: 10, marginTop: 4 }, tabBarItemStyle: { minHeight: 48 } }}>
    {[{ name: 'home', title: t("common:m_dcd2f7f89bdc"), icon: House }, { name: 'payments', title: t("common:m_c40832ab139b"), icon: WalletCards }, { name: 'calendar', title: t("common:m_5785a01da6e0"), icon: CalendarDays }, { name: 'subscriptions', title: t("common:m_305a7abea88b"), icon: Repeat2 }, { name: 'settings', title: t("common:m_80ad54ad05fa"), icon: Settings2 }].map(({ name, title, icon: Icon }) => <Tabs.Screen key={name} name={name} options={{ title, tabBarIcon: ({ color, focused }) => <Icon color={color} size={22} strokeWidth={focused ? 2.2 : 1.65}/> }}/>)}
  </Tabs>;
}
