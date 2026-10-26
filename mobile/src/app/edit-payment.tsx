import { t, useLocaleRevision } from "../i18n/runtime";
import { useLocalSearchParams } from 'expo-router';
import { z } from 'zod';
import PaymentFormScreen from '../features/payments/PaymentFormScreen';
import { AppScreen, AppHeader, EmptyState } from '../components/ui';
export default function EditPayment() {
    useLocaleRevision();
    const { id } = useLocalSearchParams<{
        id?: string;
    }>();
    return z.uuid().safeParse(id).success ? <PaymentFormScreen id={id}/> : <AppScreen><AppHeader title={t("common:m_46e308473e94")} back/><EmptyState title={t("common:m_d5f9262c7b68")}/></AppScreen>;
}
