import { t, useLocaleRevision } from "../i18n/runtime";
import { router } from 'expo-router';
import { AppScreen, EmptyState } from '../components/ui';
export default function NotFound() { useLocaleRevision(); return <AppScreen><EmptyState title={t("common:m_6a9128aa68d3")} message={t("common:m_a86988a50cd1")} action={t("common:m_dc1356403d5c")} onPress={() => router.replace('/')}/></AppScreen>; }
