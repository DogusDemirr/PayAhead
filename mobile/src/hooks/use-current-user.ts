import { useQuery } from '@tanstack/react-query';
import { currentUserKey, fetchCurrentUser } from '../services/auth-runtime';
import { useSessionStore } from '../store/session';
export function useCurrentUser() {
  const authenticated = useSessionStore((state) => state.status === 'authenticated');
  return useQuery({ queryKey: currentUserKey(), queryFn: ({ signal }) => fetchCurrentUser(signal), enabled: authenticated, retry: false, staleTime: 30000 });
}
