using System.Text.Json.Serialization;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Application.Subscriptions;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class SubscriptionRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? ProviderName { get; init; }
    public decimal? Amount { get; init; }
    public string? CurrencyCode { get; init; }
    public PaymentFrequency? BillingFrequency { get; init; }
    public int? BillingInterval { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? FirstBillingDate { get; init; }
    public int? AnchorDay { get; init; }
    public DateOnly? TrialStartDate { get; init; }
    public DateOnly? TrialEndDate { get; init; }
    public SubscriptionStatus? Status { get; init; }
    public bool AutoRenew { get; init; } = true;
    public DateOnly? AccessEndsOn { get; init; }
    public bool EnableTrialReminders { get; init; } = true;
    public string? RowVersion { get; init; }
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CancelSubscriptionRequest(DateOnly? AccessEndsOn, string? RowVersion);
public sealed class SubscriptionQuery
{
    public SubscriptionStatus? Status { get; init; }
    public string? ProviderName { get; init; }
    public string? CurrencyCode { get; init; }
    public string? Search { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
public sealed class ExpiringTrialsQuery { public int WithinDays { get; init; } = 7; }
public sealed record SubscriptionContext(DateOnly Today, string TimeZoneId, bool UsedTimeZoneFallback);
public sealed record SubscriptionRecord(TrackedSubscription Subscription, PaymentPlan Plan);
public sealed record SubscriptionResponse(Guid Id, Guid PaymentPlanId, string Name, string? Description, string ProviderName,
    decimal Amount, string CurrencyCode, PaymentFrequency BillingFrequency, int BillingInterval, int? AnchorDay,
    DateOnly StartDate, DateOnly FirstBillingDate, DateOnly? NextBillingDate, DateOnly? TrialStartDate, DateOnly? TrialEndDate,
    int? TrialDaysRemaining, SubscriptionStatus Status, bool AutoRenew, bool EnableTrialReminders,
    DateTime? CancellationRequestedAtUtc, DateTime? CancelledAtUtc, DateOnly? AccessEndsOn,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, string RowVersion, SubscriptionContext Context);
public sealed record ExpiringTrialResponse(Guid SubscriptionId, string Name, string ProviderName, DateOnly TrialEndDate,
    int DaysRemaining, DateOnly FirstBillingDate, decimal Amount, string CurrencyCode);
public sealed record MonthlyEquivalentTotal(string CurrencyCode, decimal EstimatedAmount);
public sealed record SubscriptionSummary(int TotalSubscriptionCount, int ActiveSubscriptionCount, int TrialSubscriptionCount,
    int CancelledSubscriptionCount, int ExpiredSubscriptionCount, IReadOnlyList<MonthlyEquivalentTotal> MonthlyEquivalentTotalsByCurrency,
    int ExpiringTrialsCount, SubscriptionResponse? NextRenewal, SubscriptionContext Context);
public interface ISubscriptionStore
{
    Task<string?> TimeZoneAsync(Guid user, CancellationToken ct);
    Task<IReadOnlyList<SubscriptionRecord>> LoadAsync(Guid user, CancellationToken ct);
    Task<SubscriptionRecord?> FindAsync(Guid user, Guid id, CancellationToken ct);
    Task<SubscriptionRecord> CreateAsync(Guid user, SubscriptionRequest request, DateTime now, DateOnly today, CancellationToken ct);
    Task<SubscriptionRecord> UpdateAsync(Guid user, Guid id, SubscriptionRequest request, DateTime now, DateOnly today, CancellationToken ct);
    Task<SubscriptionRecord> CancelAsync(Guid user, Guid id, CancelSubscriptionRequest request, DateTime now, DateOnly today, CancellationToken ct);
    Task DeleteAsync(Guid user, Guid id, byte[] version, DateTime now, CancellationToken ct);
}
public interface ISubscriptionService
{
    Task<SubscriptionResponse> CreateAsync(SubscriptionRequest request, CancellationToken ct);
    Task<SubscriptionResponse> GetAsync(Guid id, CancellationToken ct);
    Task<PagedResult<SubscriptionResponse>> ListAsync(SubscriptionQuery query, CancellationToken ct);
    Task<SubscriptionResponse> UpdateAsync(Guid id, SubscriptionRequest request, CancellationToken ct);
    Task<SubscriptionResponse> CancelAsync(Guid id, CancelSubscriptionRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, string? version, CancellationToken ct);
    Task<IReadOnlyList<ExpiringTrialResponse>> TrialsAsync(ExpiringTrialsQuery query, CancellationToken ct);
    Task<SubscriptionSummary> SummaryAsync(CancellationToken ct);
}
