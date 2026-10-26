import { t, useLocaleRevision } from "../i18n/runtime";
import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import OccurrenceScreen from '../features/occurrences/OccurrenceScreen';
import { validDate } from '../features/payments/dates';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function OccurrenceRoute() {
    useLocaleRevision();
    const { id, date } = useLocalSearchParams<{
        id?: string;
        date?: string;
    }>();
    return z.uuid().safeParse(id).success && typeof date === 'string' && validDate(date) ? <OccurrenceScreen id={id!} date={date}/> : <AppScreen><AppHeader title={t("common:m_52ba82f41da1")} back/><EmptyState title={t("common:m_9b14761462ed")}/></AppScreen>;
}
