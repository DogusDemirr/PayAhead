using PayAhead.Application.Payments;
using PayAhead.Domain;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PayAhead.Application.Overview;

public class OverviewRangeQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
}

public sealed class UpcomingPaymentsQuery : OverviewRangeQuery
{
    [DefaultValue(OccurrenceStatus.Pending)]
    public OccurrenceStatus Status { get; init; } = OccurrenceStatus.Pending;
    public string? CurrencyCode { get; init; }
    [DefaultValue(1)]
    public int PageNumber { get; init; } = 1;
    [DefaultValue(20)]
    public int PageSize { get; init; } = 20;
}

public sealed class CalendarQuery
{
    [Required]
    public int? Year { get; init; }
    [Required]
    public int? Month { get; init; }
    public OccurrenceStatus? Status { get; init; }
    public string? CurrencyCode { get; init; }
}

public static class OverviewLimits
{
    public const int MaximumDays = 366;
    public const int MaximumPlans = 1_000;
    public const int MaximumResults = 50_000;
}

public sealed record ProjectionContext(DateOnly FromDate, DateOnly ToDate, DateOnly Today, string TimeZoneId, bool UsedTimeZoneFallback);
public sealed record ProjectedPayment(Guid PaymentPlanId, string PaymentName, DateOnly DueDate, decimal Amount,
    string CurrencyCode, string Category, PaymentFrequency Frequency, OccurrenceStatus Status, decimal? ActualAmount, DateTime? PaidAtUtc)
{
    public Guid? CatalogCategoryId { get; init; }
    public Guid? CatalogServiceId { get; init; }
}
public sealed record CurrencyTotal(string CurrencyCode, decimal Amount);
public sealed record UpcomingPaymentsResponse(ProjectionContext Context, IReadOnlyList<ProjectedPayment> Items,
    int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => Pagination.TotalPages(TotalCount, PageSize);
}
public sealed record DashboardSummaryResponse(ProjectionContext Context, int ActivePaymentPlanCount,
    int UpcomingPaymentCount, int PaidPaymentCount, int SkippedPaymentCount, int PendingPaymentCount,
    IReadOnlyList<CurrencyTotal> UpcomingTotalsByCurrency, IReadOnlyList<CurrencyTotal> PaidTotalsByCurrency,
    ProjectedPayment? NextUpcomingPayment);
public sealed record CalendarDayResponse(DateOnly Date, int PaymentCount, IReadOnlyList<CurrencyTotal> TotalsByCurrency,
    IReadOnlyList<ProjectedPayment> Payments);
public sealed record CalendarResponse(int Year, int Month, ProjectionContext Context, IReadOnlyList<CalendarDayResponse> Days);
public sealed record OverviewSnapshot(IReadOnlyList<PaymentPlan> Plans, IReadOnlyList<PaymentOccurrence> Occurrences, int ActivePaymentPlanCount);

public interface IOverviewStore
{
    Task<string?> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<OverviewSnapshot> LoadAsync(Guid userId, DateOnly from, DateOnly to, string? currencyCode, bool includeActiveCount, CancellationToken cancellationToken);
}

public interface IOverviewService
{
    Task<UpcomingPaymentsResponse> GetUpcomingAsync(UpcomingPaymentsQuery query, CancellationToken cancellationToken);
    Task<DashboardSummaryResponse> GetSummaryAsync(OverviewRangeQuery query, CancellationToken cancellationToken);
    Task<CalendarResponse> GetCalendarAsync(CalendarQuery query, CancellationToken cancellationToken);
}
