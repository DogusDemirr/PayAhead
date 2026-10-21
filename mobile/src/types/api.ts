// Reviewed against Application/Overview, Payments, Auth and Notifications/Contracts.cs.
// Phase 1 uses UI preview models separately; no network request is sent by a screen.
export type DateOnly = string;
export type CurrencyCode = string;
export type PaymentFrequency = 'Once' | 'Daily' | 'Weekly' | 'Monthly' | 'Yearly';
export type OccurrenceStatus = 'Pending' | 'Paid' | 'Skipped';
export interface ProjectedPayment {
  paymentPlanId: string; paymentName: string; dueDate: DateOnly; amount: number;
  currencyCode: CurrencyCode; category: string; frequency: PaymentFrequency;
  status: OccurrenceStatus; actualAmount: number | null; paidAtUtc: string | null;
}
export interface CurrencyTotal { currencyCode: CurrencyCode; amount: number }
export interface ProjectionContext { fromDate: DateOnly; toDate: DateOnly; today: DateOnly; timeZoneId: string; usedTimeZoneFallback: boolean }
export interface DashboardSummaryResponse {
  context: ProjectionContext; activePaymentPlanCount: number; upcomingPaymentCount: number;
  paidPaymentCount: number; skippedPaymentCount: number; pendingPaymentCount: number;
  upcomingTotalsByCurrency: CurrencyTotal[]; paidTotalsByCurrency: CurrencyTotal[]; nextUpcomingPayment: ProjectedPayment | null;
}
export interface UserResponse { id: string; email: string; displayName: string }
export interface AuthResponse {
  accessToken: string; refreshToken: string; accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string; user: UserResponse; tokenType: 'Bearer';
}
export interface ProblemDetails { type?: string; title?: string; status?: number; detail?: string; traceId?: string; code?: string; errors?: Record<string, string[]> }
