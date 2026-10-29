import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { CanceledError } from 'axios';
import { apiClient } from '../../services/auth-runtime';
import { useFinanceScope } from '../payments/queries';
export const securitySchema = z.object({ remainingChanges: z.number().int().min(0).max(3), changeLimit: z.literal(3), windowDays: z.literal(30), nextAvailableAtUtc: z.iso.datetime({ offset: true }).nullable(), asOfUtc: z.iso.datetime({ offset: true }), activeSessions: z.number().int().nonnegative() });
export function useSecuritySummary() {
  const scope = useFinanceScope();
  return useQuery({ queryKey: [...scope.key, 'security'], enabled: scope.enabled, retry: false, staleTime: 15000,
    queryFn: async ({ signal }) => { const response = await apiClient.get('/api/users/me/security', { signal, ...{ sessionEpoch: scope.context.epoch } }); if (!scope.context.isCurrent()) throw new CanceledError(); return securitySchema.parse(response.data); } });
}
