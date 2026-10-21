import { router } from 'expo-router';
import { AppScreen, EmptyState } from '../components/ui';
export default function NotFound() { return <AppScreen><EmptyState title="Bu sayfayı bulamadık" message="Ana sayfaya dönerek devam edebilirsin." action="Ana sayfaya dön" onPress={() => router.replace('/')} /></AppScreen>; }
