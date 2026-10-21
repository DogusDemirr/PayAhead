import { useLocalSearchParams } from 'expo-router';
import { AppScreen, AppHeader, AppCard, AppText, Assurance } from '../components/ui';
export default function Information() { const params = useLocalSearchParams<{ title?: string; message?: string }>(); return <AppScreen><AppHeader title={params.title ?? 'Bilgi'} back /><AppCard><AppText>{params.message ?? 'Bu özellik henüz kullanılamıyor.'}</AppText></AppCard><Assurance /></AppScreen>; }
