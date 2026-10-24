import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import SubscriptionDetailScreen from '../features/subscriptions/SubscriptionDetailScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function SubscriptionRoute() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  return z.uuid().safeParse(id).success ? <SubscriptionDetailScreen id={id!} /> : <AppScreen><AppHeader title="Abonelik detayı" back /><EmptyState title="Geçerli bir abonelik seç" /></AppScreen>;
}
