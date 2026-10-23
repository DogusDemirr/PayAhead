using FluentValidation;
using PayAhead.Domain;

namespace PayAhead.Application.Overview;

public sealed record AnalyticsBucket(string Label, decimal Amount, int Count);
public sealed record AnalyticsMonth(DateOnly Month, decimal PlannedAmount, decimal PaidAmount, decimal PendingAmount, decimal OverdueAmount,
    int PaidCount, int PendingCount, int SkippedCount);
public sealed record CurrencyAnalytics(string CurrencyCode, IReadOnlyList<AnalyticsMonth> Months,
    IReadOnlyList<AnalyticsBucket> PaidCategories, IReadOnlyList<AnalyticsBucket> TopPlans);
public sealed record AnalyticsResponse(ProjectionContext Context, IReadOnlyList<CurrencyAnalytics> Currencies);
public interface IAnalyticsService
{
    Task<AnalyticsResponse> GetAnalyticsAsync(CalendarQuery query, CancellationToken ct);
}
public sealed partial class OverviewService : IAnalyticsService
{
    public async Task<AnalyticsResponse> GetAnalyticsAsync(CalendarQuery query, CancellationToken ct)
    {
        var userId = UserId;
        await calendarValidator.ValidateAndThrowAsync(query, ct);
        if (query.Status is not null) throw new DomainRuleException("Analiz tüm durumları karşılaştırır; status filtresi desteklenmiyor.");
        var selected = new DateOnly(query.Year!.Value, query.Month!.Value, 1);
        if (selected.Year == 1 && selected.Month < 6) throw new DomainRuleException("Analiz için en az Haziran 0001 seçilmelidir.");
        var from = selected.AddMonths(-5); var to = new DateOnly(selected.Year, selected.Month, DateTime.DaysInMonth(selected.Year, selected.Month));
        var context = await ContextAsync(userId, new OverviewRangeQuery { FromDate = from, ToDate = to }, ct);
        var snapshot = await store.LoadAsync(userId, from, to, Currency(query.CurrencyCode), false, ct);
        var plans = snapshot.Plans.ToDictionary(x => x.Id);
        var projected = Project(snapshot, context);
        var currencies = projected.GroupBy(x => x.CurrencyCode).OrderBy(x => x.Key).Select(currency =>
        {
            var months = Enumerable.Range(0, 6).Select(offset =>
            {
                var month = from.AddMonths(offset);
                var rows = currency.Where(x => x.DueDate.Year == month.Year && x.DueDate.Month == month.Month).ToArray();
                return new AnalyticsMonth(month, rows.Sum(x => plans[x.PaymentPlanId].Amount),
                    rows.Where(x => x.Status == OccurrenceStatus.Paid).Sum(x => x.ActualAmount!.Value),
                    rows.Where(x => x.Status == OccurrenceStatus.Pending).Sum(x => x.Amount),
                    rows.Where(x => x.Status == OccurrenceStatus.Pending && x.DueDate < context.Today).Sum(x => x.Amount),
                    rows.Count(x => x.Status == OccurrenceStatus.Paid), rows.Count(x => x.Status == OccurrenceStatus.Pending), rows.Count(x => x.Status == OccurrenceStatus.Skipped));
            }).ToArray();
            var current = currency.Where(x => x.DueDate >= selected).ToArray();
            var categories = current.Where(x => x.Status == OccurrenceStatus.Paid).GroupBy(x => x.Category)
                .Select(x => new AnalyticsBucket(x.Key, x.Sum(p => p.ActualAmount!.Value), x.Count())).OrderByDescending(x => x.Amount).ThenBy(x => x.Label).ToArray();
            var top = current.GroupBy(x => x.PaymentPlanId).Select(x => new AnalyticsBucket(plans[x.Key].Name, x.Sum(p => plans[p.PaymentPlanId].Amount), x.Count()))
                .OrderByDescending(x => x.Amount).ThenBy(x => x.Label).Take(5).ToArray();
            return new CurrencyAnalytics(currency.Key, months, categories, top);
        }).ToArray();
        return new(context, currencies);
    }
}
