using FluentValidation;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;

namespace PayAhead.Application.Overview;

public sealed class OverviewService(IOverviewStore store, ICurrentUser currentUser, IPaymentRecurrenceCalculator calculator,
    TimeProvider clock, IValidator<UpcomingPaymentsQuery> upcomingValidator, IValidator<OverviewRangeQuery> summaryValidator,
    IValidator<CalendarQuery> calendarValidator) : IOverviewService
{
    private Guid UserId => currentUser.UserId is { } id && id != Guid.Empty ? id : throw new AuthenticationRequiredException();

    public async Task<UpcomingPaymentsResponse> GetUpcomingAsync(UpcomingPaymentsQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await upcomingValidator.ValidateAndThrowAsync(query, cancellationToken);
        var context = await ContextAsync(userId, query, cancellationToken);
        var snapshot = await store.LoadAsync(userId, context.FromDate, context.ToDate, Currency(query.CurrencyCode), false, cancellationToken);
        var payments = Project(snapshot, context).Where(x => x.Status == query.Status).ToArray();
        return new(context, payments.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToArray(),
            query.PageNumber, query.PageSize, payments.Length);
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(OverviewRangeQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await summaryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var context = await ContextAsync(userId, query, cancellationToken);
        var snapshot = await store.LoadAsync(userId, context.FromDate, context.ToDate, null, true, cancellationToken);
        var payments = Project(snapshot, context);
        var pending = payments.Where(x => x.Status == OccurrenceStatus.Pending).ToArray();
        var upcoming = pending.Where(x => x.DueDate >= context.Today).ToArray();
        var paid = payments.Where(x => x.Status == OccurrenceStatus.Paid).ToArray();
        return new(context, snapshot.ActivePaymentPlanCount, upcoming.Length, paid.Length,
            payments.Count(x => x.Status == OccurrenceStatus.Skipped), pending.Length, Totals(upcoming), Totals(paid), upcoming.FirstOrDefault());
    }

    public async Task<CalendarResponse> GetCalendarAsync(CalendarQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId;
        await calendarValidator.ValidateAndThrowAsync(query, cancellationToken);
        var year = query.Year!.Value; var month = query.Month!.Value;
        var range = new OverviewRangeQuery { FromDate = new(year, month, 1), ToDate = new(year, month, DateTime.DaysInMonth(year, month)) };
        var context = await ContextAsync(userId, range, cancellationToken);
        var snapshot = await store.LoadAsync(userId, context.FromDate, context.ToDate, Currency(query.CurrencyCode), false, cancellationToken);
        var payments = Project(snapshot, context).Where(x => query.Status is null || x.Status == query.Status);
        var days = payments.GroupBy(x => x.DueDate).OrderBy(x => x.Key).Select(group => new CalendarDayResponse(group.Key,
            group.Count(), Totals(group.Where(x => x.Status == OccurrenceStatus.Pending)), group.ToArray())).ToArray();
        return new(year, month, context, days);
    }

    private async Task<ProjectionContext> ContextAsync(Guid userId, OverviewRangeQuery query, CancellationToken cancellationToken)
    {
        var zoneId = await store.GetTimeZoneIdAsync(userId, cancellationToken);
        var fallback = string.IsNullOrWhiteSpace(zoneId);
        var zone = TimeZoneInfo.Utc;
        if (!fallback)
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId!); }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException) { fallback = true; }
        }
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), zone).DateTime);
        var from = query.FromDate ?? today;
        var to = query.ToDate ?? DateOnly.FromDayNumber(Math.Min(DateOnly.MaxValue.DayNumber, from.DayNumber + 30));
        if (from > to || to.DayNumber - from.DayNumber >= OverviewLimits.MaximumDays)
            throw new DomainRuleException("Tarih aralığı sıralı olmalı ve en fazla 366 dahil gün içermelidir.");
        return new(from, to, today, zone.Id, fallback);
    }

    private IReadOnlyList<ProjectedPayment> Project(OverviewSnapshot snapshot, ProjectionContext context)
    {
        if (snapshot.Plans.Count > OverviewLimits.MaximumPlans || snapshot.Occurrences.Count > OverviewLimits.MaximumResults) throw Limit();
        var plans = snapshot.Plans.ToDictionary(x => x.Id);
        var stored = snapshot.Occurrences.ToDictionary(x => (x.PaymentPlanId, x.DueDate));
        var payments = new Dictionary<(Guid, DateOnly), ProjectedPayment>();
        foreach (var occurrence in snapshot.Occurrences)
        {
            if (occurrence.Status is OccurrenceStatus.Paid or OccurrenceStatus.Skipped)
                Add(plans[occurrence.PaymentPlanId], occurrence.DueDate, occurrence);
        }
        foreach (var plan in snapshot.Plans.Where(x => x.IsActive))
        {
            foreach (var date in calculator.CalculateOccurrences(plan, context.FromDate, context.ToDate))
                Add(plan, date, stored.GetValueOrDefault((plan.Id, date)));
        }
        return payments.Values.OrderBy(x => x.DueDate).ThenBy(x => x.PaymentName, StringComparer.Ordinal)
            .ThenBy(x => x.PaymentPlanId).ToArray();

        void Add(PaymentPlan plan, DateOnly date, PaymentOccurrence? occurrence)
        {
            var key = (plan.Id, date);
            if (!payments.ContainsKey(key) && payments.Count == OverviewLimits.MaximumResults) throw Limit();
            var status = occurrence?.Status ?? OccurrenceStatus.Pending;
            payments[key] = new(plan.Id, plan.Name, date,
                status == OccurrenceStatus.Paid ? occurrence!.ActualAmount!.Value : plan.Amount,
                plan.CurrencyCode, plan.Category, plan.Frequency, status, occurrence?.ActualAmount,
                occurrence?.PaidAtUtc is { } paidAt ? DateTime.SpecifyKind(paidAt, DateTimeKind.Utc) : null);
        }
    }

    private static CurrencyTotal[] Totals(IEnumerable<ProjectedPayment> payments) => payments.GroupBy(x => x.CurrencyCode, StringComparer.Ordinal)
        .OrderBy(x => x.Key, StringComparer.Ordinal).Select(group => new CurrencyTotal(group.Key, group.Sum(x => x.Amount))).ToArray();
    private static string? Currency(string? code) => code?.Trim().ToUpperInvariant();
    private static DomainRuleException Limit() => new("İstek en fazla 1000 ilgili plan ve 50000 birleşik sonuç içerebilir; aralığı veya para birimini daraltın.");
}
