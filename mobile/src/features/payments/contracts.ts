import { z } from 'zod';
import { validDate } from './dates';
export const frequencySchema = z.enum(['Once', 'Daily', 'Weekly', 'Monthly', 'Yearly']);
const date = z.string().refine(validDate);
const decimal = z.string().regex(/^-?\d+(\.\d{1,4})?$/);
export const paymentSchema = z.object({
  catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional(),
  id: z.uuid(), name: z.string(), description: z.string().nullable(), amount: decimal, currencyCode: z.string(), category: z.string(),
  frequency: frequencySchema, interval: z.number().int().min(1), startDate: date, endDate: date.nullable(), anchorDay: z.number().int().nullable(),
  isActive: z.boolean(), createdAtUtc: z.string(), updatedAtUtc: z.string(), rowVersion: z.string(),
});
export type PaymentPlan = z.infer<typeof paymentSchema>;
export type PaymentWrite = Pick<PaymentPlan, 'name' | 'description' | 'amount' | 'currencyCode' | 'category' | 'frequency' | 'interval' | 'startDate' | 'endDate' | 'anchorDay' | 'isActive' | 'catalogCategoryId' | 'catalogServiceId'>;
export const pageSchema = <T extends z.ZodType>(item: T) => z.object({ items: z.array(item), pageNumber: z.number().int(), pageSize: z.number().int(), totalCount: z.number().int(), totalPages: z.number().int() });
export const contextSchema = z.object({ fromDate: date, toDate: date, today: date, timeZoneId: z.string(), usedTimeZoneFallback: z.boolean() });
export const totalSchema = z.object({ currencyCode: z.string(), amount: decimal });
export const projectedSchema = z.object({ catalogCategoryId: z.uuid().nullable().optional(), catalogServiceId: z.uuid().nullable().optional(), paymentPlanId: z.uuid(), paymentName: z.string(), dueDate: date, amount: decimal, currencyCode: z.string(), category: z.string(), frequency: frequencySchema, status: z.enum(['Pending', 'Paid', 'Skipped']), actualAmount: decimal.nullable(), paidAtUtc: z.string().nullable() });
export const summarySchema = z.object({ context: contextSchema, activePaymentPlanCount: z.number().int(), upcomingPaymentCount: z.number().int(), paidPaymentCount: z.number().int(), skippedPaymentCount: z.number().int(), pendingPaymentCount: z.number().int(), upcomingTotalsByCurrency: z.array(totalSchema), paidTotalsByCurrency: z.array(totalSchema), nextUpcomingPayment: projectedSchema.nullable() });
export const upcomingSchema = pageSchema(projectedSchema).extend({ context: contextSchema });
export const entitlementSchema = z.object({ plan: z.string(), state: z.string(), isPremium: z.boolean(), limits: z.object({ activePaymentPlans: z.number().int().nonnegative(), activeRemindersPerPlan: z.number().int(), activeTrackedSubscriptions: z.number().int() }) });
export type Summary = z.infer<typeof summarySchema>;
export type Projected = z.infer<typeof projectedSchema>;
export type Entitlement = z.infer<typeof entitlementSchema>;
export const occurrenceStatusSchema = z.enum(['Pending', 'Paid', 'Skipped']);
export type OccurrenceStatus = z.infer<typeof occurrenceStatusSchema>;
export const occurrenceSchema = z.object({ id: z.uuid().nullable(), paymentPlanId: z.uuid(), dueDate: date, status: occurrenceStatusSchema, actualAmount: decimal.nullable(), paidAtUtc: z.string().nullable(), createdAtUtc: z.string().nullable(), rowVersion: z.string().nullable(), isRecorded: z.boolean() });
export type Occurrence = z.infer<typeof occurrenceSchema>;
export const calendarSchema = z.object({ year: z.number(), month: z.number(), context: contextSchema, days: z.array(z.object({ date, paymentCount: z.number(), totalsByCurrency: z.array(totalSchema), payments: z.array(projectedSchema) })) });
const bucketSchema = z.object({ label: z.string(), amount: decimal, count: z.number() });
const monthSchema = z.object({ month: date, plannedAmount: decimal, paidAmount: decimal, pendingAmount: decimal, overdueAmount: decimal, paidCount: z.number(), pendingCount: z.number(), skippedCount: z.number() });
export const analyticsSchema = z.object({ context: contextSchema, currencies: z.array(z.object({ currencyCode: z.string(), months: z.array(monthSchema), paidCategories: z.array(bucketSchema), topPlans: z.array(bucketSchema) })) });
export type CurrencyAnalytics = z.infer<typeof analyticsSchema>['currencies'][number];
export interface PlanFilter { pageNumber: number; pageSize: number; search?: string; isActive?: boolean; currencyCode?: string; category?: string }
