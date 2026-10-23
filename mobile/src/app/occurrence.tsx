import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import OccurrenceScreen from '../features/occurrences/OccurrenceScreen';
import { validDate } from '../features/payments/dates';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function OccurrenceRoute() {
  const { id, date } = useLocalSearchParams<{ id?: string; date?: string }>();
  return z.uuid().safeParse(id).success && typeof date === 'string' && validDate(date) ? <OccurrenceScreen id={id!} date={date} /> : <AppScreen><AppHeader title="Tek ödeme" back /><EmptyState title="Geçerli bir ödeme tarihi seç" /></AppScreen>;
}
