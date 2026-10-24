import { z } from 'zod';
import { useLocalSearchParams } from 'expo-router';
import RemindersScreen from '../features/notifications/RemindersScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function RemindersRoute() { const { id } = useLocalSearchParams<{ id?: string }>(); return z.uuid().safeParse(id).success ? <RemindersScreen plan={id!} /> : <AppScreen><AppHeader title="Hatırlatmalar" back /><EmptyState title="Geçerli bir ödeme planı seç" /></AppScreen>; }
