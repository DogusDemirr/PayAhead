import { t, useLocaleRevision } from "../i18n/runtime";
import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import SubscriptionDetailScreen from '../features/subscriptions/SubscriptionDetailScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function SubscriptionRoute() {
    useLocaleRevision();
    const { id } = useLocalSearchParams<{
        id?: string;
    }>();
    return z.uuid().safeParse(id).success ? <SubscriptionDetailScreen id={id!}/> : <AppScreen><AppHeader title={t("common:m_5a3414a08005")} back/><EmptyState title={t("common:m_6bc63db3c9c3")}/></AppScreen>;
}
