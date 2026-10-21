import { createApiClient, ApiError } from './api';
import { createAuthApi } from './auth-api';
import { SessionManager } from './session-manager';
import { tokenStorage } from './token-storage';
import { queryClient } from './query-client';
import { useSessionStore } from '../store/session';
import type { UserResponse } from '../types/api';

const baseURL = process.env.EXPO_PUBLIC_API_BASE_URL;
export const authApi = createAuthApi(createApiClient(baseURL));
export const currentUserKey = (): readonly ['current-user', number | undefined] => ['current-user', session.credential()?.epoch];
export const session: SessionManager = new SessionManager({
  api: authApi, storage: tokenStorage,
  publish: (snapshot) => useSessionStore.setState(snapshot),
  clearCache: () => { void queryClient.cancelQueries(); queryClient.clear(); },
  getUser: () => queryClient.fetchQuery({ queryKey: currentUserKey(), queryFn: ({ signal }) => fetchCurrentUser(signal), retry: false }),
});
export const apiClient = createApiClient(baseURL, session);
export async function fetchCurrentUser(signal?: AbortSignal) {
  const epoch = session.credential()?.epoch;
  try { return (await apiClient.get<UserResponse>('/api/users/me', { signal })).data; }
  catch (error) {
    if (error instanceof ApiError && [401, 403, 404].includes(error.status ?? 0) && session.credential()?.epoch === epoch)
      await session.invalidate('Hesabın artık kullanılamıyor. Yeniden giriş yap.');
    throw error;
  }
}
