import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CanceledError } from 'axios';
import { useSessionStore } from '../../store/session';
import { apiClient, session } from '../../services/auth-runtime';
import { createFinanceApi } from './api';
import { monthRange, shiftDay } from './dates';
import type { PaymentWrite, PlanFilter, OccurrenceStatus } from './contracts';
export function useCalendar(year: number, month: number) {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'calendar', year, month], queryFn: ({ signal }) => scope.api.calendar(year, month, signal), enabled: scope.enabled });
}
export function useAnalytics(year: number, month: number, currencyCode?: string) {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'analytics', year, month, currencyCode], queryFn: ({ signal }) => scope.api.analytics(year, month, signal, currencyCode), enabled: scope.enabled });
}
export function useHistory(fromDate: string, toDate: string, status: OccurrenceStatus, pageNumber: number, enabled = true) {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'history', fromDate, toDate, status, pageNumber], queryFn: ({ signal }) => scope.api.upcoming({ fromDate, toDate, status, pageNumber, pageSize: 20 }, signal), enabled: scope.enabled && enabled && fromDate <= toDate });
}
export function useOccurrence(id: string, date: string) {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'occurrence', id, date], queryFn: ({ signal }) => scope.api.occurrence(id, date, signal), enabled: scope.enabled });
}
export function useOccurrenceMutation() {
  const scope = useFinanceScope(); const client = useQueryClient();
  return useMutation({ retry: false, gcTime: 0, mutationFn: (input: { id: string; date: string; status: OccurrenceStatus; rowVersion: string | null; amount?: string }) => scope.api.setOccurrence(input.id, input.date, input.status, input.rowVersion, input.amount),
    onSuccess: async () => { await invalidateFinance(client, scope.key); if (session.credential()?.epoch !== scope.key[2]) throw new CanceledError('Oturum değişti.'); },
  });
}
export type FinanceScope = readonly ['finance', string, number | undefined];
export function financeKey(userId: string, epoch?: number): FinanceScope { return ['finance', userId, epoch]; }
export function useFinanceScope() {
  const state = useSessionStore(); const epoch = session.credential()?.epoch;
  const key = financeKey(state.user?.id ?? 'anonymous', epoch);
  const enabled = state.status === 'authenticated' && !!state.user && epoch !== undefined;
  const context = { epoch, isCurrent: () => enabled && session.credential()?.epoch === epoch, prepareWrite: () => session.ensureFreshAccess() };
  const api = createFinanceApi(apiClient, context);
  return { key, enabled, api, context };
}
export function useFinanceClock() {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'summary', 'rolling'], queryFn: ({ signal }) => scope.api.summary(undefined, signal), enabled: scope.enabled });
}
export function useDashboard() {
  const scope = useFinanceScope(); const client = useQueryClient(); const clock = useFinanceClock(); const today = clock.data?.context.today;
  const range = monthRange(today ?? '2000-01-01');
  const summary = useQuery({ queryKey: [...scope.key, 'summary', range], queryFn: ({ signal }) => scope.api.summary(range, signal), enabled: scope.enabled && !!today });
  const upcoming = useQuery({ queryKey: [...scope.key, 'upcoming', today], queryFn: ({ signal }) => scope.api.upcoming({ fromDate: today!, toDate: clock.data!.context.toDate, pageNumber: 1, pageSize: 3, status: 'Pending' }, signal), enabled: scope.enabled && !!today });
  const hasOverdueRange = !!today && range.fromDate < today;
  const overdue = useQuery({ queryKey: [...scope.key, 'overdue', today], queryFn: ({ signal }) => scope.api.upcoming({ fromDate: range.fromDate, toDate: shiftDay(today!, -1), pageNumber: 1, pageSize: 3, status: 'Pending' }, signal), enabled: scope.enabled && hasOverdueRange });
  return { clock, summary, upcoming, overdue, hasOverdueRange, async refresh() { await client.invalidateQueries({ queryKey: [...scope.key, 'subscriptions'] }); if (!today) { await clock.refetch(); return; } await Promise.all([clock.refetch(), summary.refetch(), upcoming.refetch(), ...(hasOverdueRange ? [overdue.refetch()] : [])]); } };
}
export function usePaymentList(filter: PlanFilter) {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'plans', filter], queryFn: ({ signal }) => scope.api.list(filter, signal), enabled: scope.enabled });
}
export function usePayment(id?: string) {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'plan', id], queryFn: ({ signal }) => scope.api.detail(id!, signal), enabled: scope.enabled && !!id });
}
export function useEntitlements() {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'entitlements'], queryFn: ({ signal }) => scope.api.entitlements(signal), enabled: scope.enabled });
}
export async function invalidateFinance(client: ReturnType<typeof useQueryClient>, key: FinanceScope) {
  await client.invalidateQueries({ queryKey: key });
}
export function usePaymentMutation() {
  const scope = useFinanceScope(); const client = useQueryClient();
  return useMutation({ retry: false, gcTime: 0, mutationFn: (input: { kind: 'create'; body: PaymentWrite } | { kind: 'update'; id: string; body: PaymentWrite; rowVersion: string } | { kind: 'delete'; id: string; rowVersion: string }) => input.kind === 'create' ? scope.api.create(input.body) : input.kind === 'update' ? scope.api.update(input.id, input.body, input.rowVersion) : scope.api.remove(input.id, input.rowVersion),
    onSuccess: async () => {
      await invalidateFinance(client, scope.key);
      if (session.credential()?.epoch !== scope.key[2]) throw new CanceledError('Oturum değişti.');
    },
  });
}
