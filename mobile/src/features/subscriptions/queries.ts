import { t } from "../../i18n/runtime";
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CanceledError } from 'axios';
import { apiClient, session } from '../../services/auth-runtime';
import { useFinanceScope, invalidateFinance } from '../payments/queries';
import { createSubscriptionApi } from './api';
import type { SubscriptionFilter, SubscriptionWrite } from './contracts';
function useSubscriptionScope() { const scope = useFinanceScope(); return { ...scope, subscriptions: createSubscriptionApi(apiClient, scope.context) }; }
export function useSubscriptions(filter: SubscriptionFilter) {
    const scope = useSubscriptionScope();
    return useQuery({ queryKey: [...scope.key, 'subscriptions', 'list', filter], queryFn: ({ signal }) => scope.subscriptions.list(filter, signal), enabled: scope.enabled });
}
export function useSubscription(id?: string) {
    const scope = useSubscriptionScope();
    return useQuery({ queryKey: [...scope.key, 'subscriptions', 'detail', id], queryFn: ({ signal }) => scope.subscriptions.detail(id!, signal), enabled: scope.enabled && !!id });
}
export function useSubscriptionSummary() {
    const scope = useSubscriptionScope();
    return useQuery({ queryKey: [...scope.key, 'subscriptions', 'summary'], queryFn: ({ signal }) => scope.subscriptions.summary(signal), enabled: scope.enabled });
}
export function useExpiringTrials(withinDays = 7) {
    const scope = useSubscriptionScope();
    return useQuery({ queryKey: [...scope.key, 'subscriptions', 'trials', withinDays], queryFn: ({ signal }) => scope.subscriptions.trials(withinDays, signal), enabled: scope.enabled });
}
export type SubscriptionMutation = {
    kind: 'create';
    body: SubscriptionWrite;
} | {
    kind: 'update';
    id: string;
    body: SubscriptionWrite;
    rowVersion: string;
} | {
    kind: 'cancel';
    id: string;
    accessEndsOn: string;
    rowVersion: string;
} | {
    kind: 'delete';
    id: string;
    rowVersion: string;
};
export function useSubscriptionMutation() {
    const scope = useSubscriptionScope();
    const client = useQueryClient();
    return useMutation({ retry: false, gcTime: 0, mutationFn: (input: SubscriptionMutation) => {
            if (input.kind === 'create')
                return scope.subscriptions.create(input.body);
            if (input.kind === 'update')
                return scope.subscriptions.update(input.id, input.body, input.rowVersion);
            if (input.kind === 'cancel')
                return scope.subscriptions.cancel(input.id, input.accessEndsOn, input.rowVersion);
            return scope.subscriptions.remove(input.id, input.rowVersion);
        }, onSuccess: async () => { await invalidateFinance(client, scope.key); if (session.credential()?.epoch !== scope.key[2])
            throw new CanceledError(t("subscriptions:m_72ec2727147f")); } });
}
