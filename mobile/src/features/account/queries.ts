import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CanceledError } from 'axios';
import { z } from 'zod';
import { apiClient, currentUserKey, session } from '../../services/auth-runtime';
import { useFinanceScope } from '../payments/queries';
import { t } from '../../i18n/runtime';
import { ApiError } from '../../services/api-error';

export const profileSchema = z.object({ hasAvatar: z.boolean().default(false), id: z.uuid(), email: z.string(), displayName: z.string(), rowVersion: z.string().min(1) });
export type Profile = z.infer<typeof profileSchema>;
export function useProfile() {
  const s = useFinanceScope();
  return useQuery({ queryKey: [...s.key, 'profile'], enabled: s.enabled, retry: false,
    queryFn: async ({ signal }) => {
      const profile = profileSchema.parse((await apiClient.get('/api/users/me/profile', { signal, ...{ sessionEpoch: s.context.epoch } })).data);
      if (profile.id !== s.key[1] || !s.context.isCurrent()) throw new CanceledError();
      return profile;
    } });
}
export type AccountWrite = { kind: 'profile'; displayName: string; rowVersion: string } | { kind: 'password'; currentPassword: string; newPassword: string; confirmation: string } | { kind: 'delete'; currentPassword: string };
export function useAccountMutation() {
  const s = useFinanceScope(); const cache = useQueryClient();
  return useMutation({ retry: false, gcTime: 0, mutationFn: async (value: AccountWrite) => {
    const check = () => { if (!s.context.isCurrent()) throw new CanceledError(); };
    check(); await s.context.prepareWrite(); check();
    const config = { headers: {}, ...{ sessionEpoch: s.context.epoch } };
    if (value.kind === 'profile') {
      await cache.cancelQueries({ queryKey: currentUserKey() });
      const { displayName, rowVersion } = value;
      const response = await apiClient.put('/api/users/me/profile', { displayName, rowVersion }, config); check();
      const parsed = profileSchema.safeParse(response.data); if (!parsed.success) throw new ApiError(t('settings:profile.requestFailed'));
      const user = parsed.data;
      const publicUser = { id: user.id, email: user.email, displayName: user.displayName, hasAvatar: user.hasAvatar, avatarVersion: user.rowVersion };
      session.updateUser(publicUser, s.context.epoch!);
      cache.setQueryData(currentUserKey(), publicUser);
      cache.setQueryData([...s.key, 'profile'], user);
      void cache.invalidateQueries({ queryKey: currentUserKey() }).catch(() => undefined);
    } else {
      if (value.kind === 'password') {
        const { currentPassword, newPassword, confirmation } = value;
        await apiClient.post('/api/users/me/change-password', { currentPassword, newPassword, confirmation }, config);
      } else await apiClient.delete('/api/users/me', { ...config, data: { currentPassword: value.currentPassword } });
      check(); await session.invalidate(t(value.kind === 'password' ? 'settings:profile.passwordChanged' : 'settings:profile.deleteSuccess'));
    }
  } });
}
