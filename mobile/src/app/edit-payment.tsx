import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import PaymentFormScreen from '../features/payments/PaymentFormScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function EditPayment() {
  const { id } = useLocalSearchParams<{ id?: string }>();
  return z.uuid().safeParse(id).success ? <PaymentFormScreen id={id} /> : <AppScreen><AppHeader title="Planı düzenle" back /><EmptyState title="Geçerli bir plan seç" /></AppScreen>;
}
