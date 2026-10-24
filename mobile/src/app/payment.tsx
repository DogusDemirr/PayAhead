import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import PaymentDetailScreen from '../features/payments/PaymentDetailScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function Payment() {
  const { id, readOnly } = useLocalSearchParams<{ id?: string; readOnly?: string }>();
  return z.uuid().safeParse(id).success ? <PaymentDetailScreen id={id!} readOnly={readOnly === 'true'} /> : <AppScreen><AppHeader title="Ödeme detayı" back /><EmptyState title="Geçerli bir plan seç" /></AppScreen>;
}
