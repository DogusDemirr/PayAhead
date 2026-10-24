import type { Subscription, SubscriptionSummary } from '../src/features/subscriptions/contracts';
export const subscription: Subscription = {
  id: 'a1111111-1111-4111-8111-111111111111', paymentPlanId: 'b1111111-1111-4111-8111-111111111111', name: 'Test Music', providerName: 'Test Provider', description: null,
  amount: '199.9001', currencyCode: 'TRY', billingFrequency: 'Monthly', billingInterval: 1, anchorDay: 31,
  startDate: '2026-10-24', firstBillingDate: '2026-11-01', nextBillingDate: '2026-11-01', trialStartDate: '2026-10-24', trialEndDate: '2026-10-31', trialDaysRemaining: 7,
  status: 'Trial', autoRenew: true, enableTrialReminders: true, cancellationRequestedAtUtc: null, cancelledAtUtc: null, accessEndsOn: null,
  createdAtUtc: '2026-10-24T00:00:00Z', updatedAtUtc: '2026-10-24T00:00:00Z', rowVersion: 'AAAAAAAAAAE=', context: { today: '2026-10-24', timeZoneId: 'Europe/Istanbul', usedTimeZoneFallback: false },
};
export const subscriptionSummary: SubscriptionSummary = {
  totalSubscriptionCount: 1, activeSubscriptionCount: 0, trialSubscriptionCount: 1, cancelledSubscriptionCount: 0, expiredSubscriptionCount: 0,
  monthlyEquivalentTotalsByCurrency: [{ currencyCode: 'TRY', estimatedAmount: '199.9001' }, { currencyCode: 'USD', estimatedAmount: '7.9900' }],
  expiringTrialsCount: 1, nextRenewal: subscription, context: subscription.context,
};
