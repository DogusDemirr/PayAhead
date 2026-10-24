import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import SubscriptionFormScreen from '../features/subscriptions/SubscriptionFormScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function EditSubscription() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  return z.uuid().safeParse(id).success ? <SubscriptionFormScreen id={id!} /> : <AppScreen><AppHeader title="Abonelik düzenleme" back /><EmptyState title="Geçerli bir abonelik seç" /></AppScreen>;
}
