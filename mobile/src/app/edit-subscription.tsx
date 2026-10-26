import { t, useLocaleRevision } from "../i18n/runtime";
import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import SubscriptionFormScreen from '../features/subscriptions/SubscriptionFormScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function EditSubscription() {
    useLocaleRevision();
    const { id } = useLocalSearchParams<{
        id?: string;
    }>();
    return z.uuid().safeParse(id).success ? <SubscriptionFormScreen id={id!}/> : <AppScreen><AppHeader title={t("common:m_a77cb430aae5")} back/><EmptyState title={t("common:m_6bc63db3c9c3")}/></AppScreen>;
}
