import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import PaymentDetailScreen from '../features/payments/PaymentDetailScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function Payment() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  return z.uuid().safeParse(id).success ? <PaymentDetailScreen id={id!} /> : <AppScreen><AppHeader title="Ödeme detayı" back /><EmptyState title="Geçerli bir plan seç" /></AppScreen>;
}
