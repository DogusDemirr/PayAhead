import { useEffect } from 'react';
import { AppState } from 'react-native';
import { session, currentUserKey } from '../services/auth-runtime';
import { queryClient } from '../services/query-client';
import { useCurrentUser } from '../hooks/use-current-user';
export function SessionBootstrap() {
  useCurrentUser();
  useEffect(() => {
    void session.restore();
    const listener = AppState.addEventListener('change', (state) => {
      if (state === 'active' && session.getSnapshot().status === 'authenticated') {
        void queryClient.invalidateQueries({ queryKey: currentUserKey() });
        void queryClient.invalidateQueries({ queryKey: ['finance'] });
      }
    });
    return () => listener.remove();
  }, []);
  return null;
}
