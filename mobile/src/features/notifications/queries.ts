import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { CanceledError } from 'axios';
import { apiClient, session } from '../../services/auth-runtime';
import { useFinanceScope, invalidateFinance } from '../payments/queries';
import { createNotificationApi } from './api';
import type { ReminderWrite, SettingsWrite } from './contracts';
function useScope() { const scope = useFinanceScope(); return { ...scope, notifications: createNotificationApi(apiClient, scope.context) }; }
export function useReminders(plan: string) { const s = useScope(); return useQuery({ queryKey: [...s.key, 'notifications', 'reminders', plan], queryFn: ({ signal }) => s.notifications.reminders(plan, signal), enabled: s.enabled && !!plan }); }
export function useNotificationSettings() { const s = useScope(); return useQuery({ queryKey: [...s.key, 'notifications', 'settings'], queryFn: ({ signal }) => s.notifications.settings(signal), enabled: s.enabled }); }
export function useDevices() { const s = useScope(); return useQuery({ queryKey: [...s.key, 'notifications', 'devices'], queryFn: ({ signal }) => s.notifications.devices(signal), enabled: s.enabled }); }
export type NotificationMutation = { kind: 'saveReminder'; plan: string; id?: string; body: ReminderWrite } | { kind: 'deleteReminder'; plan: string; id: string } | { kind: 'settings'; body: SettingsWrite } | { kind: 'deleteDevice'; id: string };
export function useNotificationMutation() {
  const s = useScope(); const client = useQueryClient();
  return useMutation({ retry: false, gcTime: 0, mutationFn: async (v: NotificationMutation) => v.kind === 'settings' ? s.notifications.saveSettings(v.body) : v.kind === 'deleteDevice' ? s.notifications.deleteDevice(v.id) : v.kind === 'deleteReminder' ? s.notifications.deleteReminder(v.plan, v.id) : s.notifications.saveReminder(v.plan, v.body, v.id),
    onSuccess: async () => { await invalidateFinance(client, s.key); if (session.credential()?.epoch !== s.key[2]) throw new CanceledError('Oturum değişti.'); },
  });
}
