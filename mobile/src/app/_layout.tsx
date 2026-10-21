import { useEffect } from 'react';
import { View } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { StatusBar } from 'expo-status-bar';
import { Stack } from 'expo-router/stack';
import { useReducedMotion } from 'react-native-reanimated';
import * as SplashScreen from 'expo-splash-screen';
import { useFonts } from 'expo-font';
import { Inter_400Regular } from '@expo-google-fonts/inter/400Regular';
import { Inter_500Medium } from '@expo-google-fonts/inter/500Medium';
import { Inter_600SemiBold } from '@expo-google-fonts/inter/600SemiBold';
import { Inter_700Bold } from '@expo-google-fonts/inter/700Bold';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '../services/query-client';
import { PreviewBanner } from '../components/preview';
import { LoadingSkeleton } from '../components/ui';
import { useTheme } from '../theme/use-theme';
import { SessionBootstrap } from '../components/session-bootstrap';
import { useSessionStore } from '../store/session';
import { navigationAccess } from '../services/navigation-policy';

void SplashScreen.preventAutoHideAsync();
export default function RootLayout() {
  const access = navigationAccess(useSessionStore((state) => state.status));
  const { colors, scheme } = useTheme(); const reducedMotion = useReducedMotion();
  const [loaded, error] = useFonts({ Inter_400Regular, Inter_500Medium, Inter_600SemiBold, Inter_700Bold });
  useEffect(() => { if (loaded || error) void SplashScreen.hideAsync(); }, [loaded, error]);
  return <GestureHandlerRootView style={{ flex: 1 }}><SafeAreaProvider><QueryClientProvider client={queryClient}><SessionBootstrap /><View style={{ flex: 1, backgroundColor: colors.background }}><SafeAreaView edges={['top']} style={{ flex: 1, width: '100%', maxWidth: 480, alignSelf: 'center', backgroundColor: colors.background }}><StatusBar style={scheme === 'dark' ? 'light' : 'dark'} /><PreviewBanner />{(!loaded && !error) || !access.ready ? <View style={{ padding: 24 }}><LoadingSkeleton /></View> : <Stack screenOptions={{ headerShown: false, contentStyle: { backgroundColor: colors.background }, animation: reducedMotion ? 'none' : 'fade' }}><Stack.Screen name="index" /><Stack.Protected guard={access.auth}><Stack.Screen name="(auth)" /></Stack.Protected><Stack.Protected guard={access.protected}><Stack.Screen name="(tabs)" /><Stack.Screen name="preview" options={{ presentation: 'modal' }} /><Stack.Screen name="add-payment" options={{ presentation: 'modal' }} /><Stack.Screen name="payment" /><Stack.Screen name="notifications" /><Stack.Screen name="information" /><Stack.Screen name="premium" /></Stack.Protected></Stack>}</SafeAreaView></View></QueryClientProvider></SafeAreaProvider></GestureHandlerRootView>;
}
