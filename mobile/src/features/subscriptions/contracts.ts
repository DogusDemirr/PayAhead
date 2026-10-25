import { z } from 'zod';
import { validDate } from '../payments/dates';
import { pageSchema } from '../payments/contracts';
const date = z.string().refine(validDate);
const decimal = z.string().regex(/^\d+(\.\d{1,4})?$/);
export const subscriptionStatusSchema = z.enum(['Trial', 'Active', 'Cancelled', 'Expired']);
export type SubscriptionStatus = z.infer<typeof subscriptionStatusSchema>;
export const subscriptionContextSchema = z.object({ today: date, timeZoneId: z.string(), usedTimeZoneFallback: z.boolean() });
export const subscriptionSchema = z.object({
  catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional(), category: z.string().optional(),
  id: z.uuid(), paymentPlanId: z.uuid(), name: z.string(), description: z.string().nullable(), providerName: z.string(), amount: decimal, currencyCode: z.string(),
  billingFrequency: z.enum(['Daily', 'Weekly', 'Monthly', 'Yearly']), billingInterval: z.number().int().positive(), anchorDay: z.number().int().nullable(), startDate: date, firstBillingDate: date, nextBillingDate: date.nullable(),
  trialStartDate: date.nullable(), trialEndDate: date.nullable(), trialDaysRemaining: z.number().int().nonnegative().nullable(), status: subscriptionStatusSchema,
  autoRenew: z.boolean(), enableTrialReminders: z.boolean(), cancellationRequestedAtUtc: z.string().nullable(), cancelledAtUtc: z.string().nullable(), accessEndsOn: date.nullable(),
  createdAtUtc: z.string(), updatedAtUtc: z.string(), rowVersion: z.string(), context: subscriptionContextSchema,
});
export type Subscription = z.infer<typeof subscriptionSchema>;
export type SubscriptionWrite = Pick<Subscription, 'name' | 'description' | 'providerName' | 'amount' | 'currencyCode' | 'billingFrequency' | 'billingInterval' | 'anchorDay' | 'startDate' | 'firstBillingDate' | 'trialStartDate' | 'trialEndDate' | 'autoRenew' | 'accessEndsOn' | 'enableTrialReminders' | 'catalogCategoryId' | 'catalogServiceId'> & { status: 'Active' | 'Trial' };
export const subscriptionPageSchema = pageSchema(subscriptionSchema);
export const trialSchema = z.object({ subscriptionId: z.uuid(), name: z.string(), providerName: z.string(), trialEndDate: date, daysRemaining: z.number().int().nonnegative(), firstBillingDate: date, amount: decimal, currencyCode: z.string() });
export type ExpiringTrial = z.infer<typeof trialSchema>;
export const subscriptionSummarySchema = z.object({ totalSubscriptionCount: z.number().int(), activeSubscriptionCount: z.number().int(), trialSubscriptionCount: z.number().int(), cancelledSubscriptionCount: z.number().int(), expiredSubscriptionCount: z.number().int(), monthlyEquivalentTotalsByCurrency: z.array(z.object({ currencyCode: z.string(), estimatedAmount: decimal })), expiringTrialsCount: z.number().int(), nextRenewal: subscriptionSchema.nullable(), context: subscriptionContextSchema });
export type SubscriptionSummary = z.infer<typeof subscriptionSummarySchema>;
export interface SubscriptionFilter { pageNumber: number; pageSize: number; status?: SubscriptionStatus; search?: string; providerName?: string; currencyCode?: string }
