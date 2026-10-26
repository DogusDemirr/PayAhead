import { t, useLocaleRevision } from "../i18n/runtime";
import { z } from 'zod';
import { useLocalSearchParams } from 'expo-router';
import RemindersScreen from '../features/notifications/RemindersScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function RemindersRoute() { useLocaleRevision(); const { id } = useLocalSearchParams<{
    id?: string;
}>(); return z.uuid().safeParse(id).success ? <RemindersScreen plan={id!}/> : <AppScreen><AppHeader title={t("common:m_4877b5ec41d9")} back/><EmptyState title={t("common:m_e68c08fd0390")}/></AppScreen>; }
