import { Redirect } from 'expo-router';
import { useSessionStore } from '../store/session';
import { LoadingSkeleton } from '../components/ui';
export default function Index() {
  const status = useSessionStore((state) => state.status);
  if (status === 'restoring') return <LoadingSkeleton />;
  return <Redirect href={status === 'authenticated' ? '/(tabs)/home' : '/(auth)/login'} />;
}
