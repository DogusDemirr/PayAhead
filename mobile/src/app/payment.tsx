import { t, useLocaleRevision } from "../i18n/runtime";
import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import PaymentDetailScreen from '../features/payments/PaymentDetailScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function Payment() {
    useLocaleRevision();
    const { id, readOnly } = useLocalSearchParams<{
        id?: string;
        readOnly?: string;
    }>();
    return z.uuid().safeParse(id).success ? <PaymentDetailScreen id={id!} readOnly={readOnly === 'true'}/> : <AppScreen><AppHeader title={t("common:m_7c72e97a2e24")} back/><EmptyState title={t("common:m_d5f9262c7b68")}/></AppScreen>;
}
