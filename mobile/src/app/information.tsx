import { t, useLocaleRevision } from "../i18n/runtime";
import { useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, Assurance } from '../components/ui';
export default function Information() {
    useLocaleRevision();
    const params = useLocalSearchParams<{
        title?: string;
        message?: string;
    }>();
    return <AppScreen><AppHeader title={params.title ?? t("common:m_abbbdafe41fa")} back/><AppCard><AppText>{params.message ?? t("common:m_c545594549bd")}</AppText></AppCard><Assurance /></AppScreen>;
}
