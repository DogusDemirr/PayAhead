using System.Diagnostics;
using FluentValidation;
using PayAhead.Application;
using PayAhead.Application.Overview;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using Xunit;
using Xunit.Abstractions;

namespace PayAhead.UnitTests;

public sealed class OverviewTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(PaymentFrequency.Once, "2026-01-31")]
    [InlineData(PaymentFrequency.Daily, "2026-01-31,2026-02-02,2026-02-04,2026-02-06,2026-02-08,2026-02-10,2026-02-12,2026-02-14,2026-02-16,2026-02-18,2026-02-20,2026-02-22,2026-02-24,2026-02-26,2026-02-28")]
    [InlineData(PaymentFrequency.Weekly, "2026-01-31,2026-02-14,2026-02-28")]
    [InlineData(PaymentFrequency.Monthly, "2026-01-31")]
    [InlineData(PaymentFrequency.Yearly, "2026-01-31")]
    public async Task Upcoming_uses_existing_recurrence_for_all_frequencies_and_intervals(PaymentFrequency frequency, string expected)
    {
        var plan = Plan(); plan.Frequency = frequency; plan.Interval = 2;
        var result = await Service(new([plan])).GetUpcomingAsync(new() { FromDate = new(2026, 1, 1), ToDate = new(2026, 2, 28), PageSize = 100 }, default);
        Assert.Equal(expected.Split(',').Select(DateOnly.Parse), result.Items.Select(x => x.DueDate));
    }

    [Theory]
    [InlineData(2026, "2026-02-28")]
    [InlineData(2024, "2024-02-29")]
    public async Task Month_end_and_leap_dates_are_not_reimplemented(int year, string expected)
    {
        var plan = Plan(); plan.StartDate = new(year, 1, 31);
        var calendar = await Service(new([plan])).GetCalendarAsync(new() { Year = year, Month = 2 }, default);
        Assert.Equal(DateOnly.Parse(expected), Assert.Single(calendar.Days).Date);
    }

    [Fact]
    public async Task Merge_avoids_duplicates_and_summary_uses_actual_paid_amount_and_separate_currencies()
    {
        var tryPlan = Plan(amount: 10.1234m); var usd = Plan("USD", 2.5m); var eur = Plan("EUR", 9);
        var store = new MemoryStore([tryPlan, usd, eur], [Occurrence(tryPlan, new(2026, 1, 31), OccurrenceStatus.Paid, 7.4321m),
            Occurrence(eur, new(2026, 1, 31), OccurrenceStatus.Skipped)]);
        var service = Service(store);
        var query = new OverviewRangeQuery { FromDate = new(2026, 1, 1), ToDate = new(2026, 2, 28) };
        var summary = await service.GetSummaryAsync(query, default);
        Assert.Equal(4, summary.PendingPaymentCount); Assert.Equal(4, summary.UpcomingPaymentCount);
        Assert.Equal(1, summary.PaidPaymentCount); Assert.Equal(1, summary.SkippedPaymentCount);
        Assert.Equal(new[] { new CurrencyTotal("EUR", 9), new CurrencyTotal("TRY", 10.1234m), new CurrencyTotal("USD", 5) }, summary.UpcomingTotalsByCurrency);
        Assert.Equal(new[] { new CurrencyTotal("TRY", 7.4321m) }, summary.PaidTotalsByCurrency);
        var paid = await service.GetUpcomingAsync(new() { FromDate = query.FromDate, ToDate = query.ToDate, Status = OccurrenceStatus.Paid }, default);
        var row = Assert.Single(paid.Items); Assert.Equal(7.4321m, row.Amount); Assert.Equal(7.4321m, row.ActualAmount);
        Assert.Equal(DateTimeKind.Utc, row.PaidAtUtc!.Value.Kind);
        Assert.Equal(usd.Id, summary.NextUpcomingPayment!.PaymentPlanId);
        Assert.Equal(2, store.Occurrences.Count); // Okuma yeni occurrence kaydetmez.
    }

    [Fact]
    public async Task Inactive_and_changed_plans_keep_saved_history_without_synthetic_pending()
    {
        var inactive = Plan(); inactive.IsActive = false;
        var changed = Plan(); changed.StartDate = new(2026, 2, 1);
        var store = new MemoryStore([inactive, changed], [Occurrence(inactive, new(2026, 1, 31), OccurrenceStatus.Paid, 5),
            Occurrence(inactive, new(2026, 2, 28), OccurrenceStatus.Skipped), Occurrence(changed, new(2026, 1, 15), OccurrenceStatus.Paid, 6)]);
        var service = Service(store);
        var summary = await service.GetSummaryAsync(new() { FromDate = new(2026, 1, 1), ToDate = new(2026, 2, 28) }, default);
        Assert.Equal(1, summary.ActivePaymentPlanCount); Assert.Equal(2, summary.PaidPaymentCount);
        Assert.Equal(1, summary.SkippedPaymentCount); Assert.Equal(1, summary.PendingPaymentCount);
        Assert.Equal(new[] { new CurrencyTotal("TRY", 11) }, summary.PaidTotalsByCurrency);
        var january = await service.GetCalendarAsync(new() { Year = 2026, Month = 1 }, default);
        Assert.Equal(new[] { new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 31) }, january.Days.Select(x => x.Date));
        Assert.All(january.Days, x => Assert.Empty(x.TotalsByCurrency));
    }

    [Fact]
    public async Task Summary_distinguishes_overdue_pending_from_upcoming_and_supports_seven_day_range()
    {
        var plan = Plan(); plan.Frequency = PaymentFrequency.Daily; plan.StartDate = new(2025, 12, 30);
        var summary = await Service(new([plan])).GetSummaryAsync(new() { FromDate = new(2025, 12, 30), ToDate = new(2026, 1, 5) }, default);
        Assert.Equal(7, summary.PendingPaymentCount); Assert.Equal(5, summary.UpcomingPaymentCount);
        Assert.Equal(new DateOnly(2026, 1, 1), summary.NextUpcomingPayment!.DueDate);
        Assert.Equal(50, Assert.Single(summary.UpcomingTotalsByCurrency).Amount);
    }

    [Fact]
    public async Task Calendar_groups_only_days_with_payments_and_pending_currency_totals()
    {
        var first = Plan("TRY", 10, "A"); var second = Plan("USD", 2, "B"); var paid = Plan("TRY", 99, "C");
        var result = await Service(new([first, second, paid], [Occurrence(paid, new(2026, 1, 31), OccurrenceStatus.Paid, 4)]))
            .GetCalendarAsync(new() { Year = 2026, Month = 1 }, default);
        var day = Assert.Single(result.Days); Assert.Equal(new DateOnly(2026, 1, 31), day.Date); Assert.Equal(3, day.PaymentCount);
        Assert.Equal(new[] { new CurrencyTotal("TRY", 10), new CurrencyTotal("USD", 2) }, day.TotalsByCurrency);
        Assert.Equal(new[] { "A", "B", "C" }, day.Payments.Select(x => x.PaymentName));
    }

    [Fact]
    public async Task Sorting_uses_due_date_ordinal_name_and_guid_before_pagination()
    {
        var low = Plan(name: "Same"); low.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var high = Plan(name: "Same"); high.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var a = Plan(name: "A");
        var service = Service(new([high, low, a]));
        var result = await service.GetUpcomingAsync(new() { FromDate = new(2026, 1, 31), ToDate = new(2026, 1, 31), PageSize = 1, PageNumber = 2 }, default);
        Assert.Equal(low.Id, Assert.Single(result.Items).PaymentPlanId); Assert.Equal(3, result.TotalPages); Assert.Equal(3, result.TotalCount);
        var again = await service.GetUpcomingAsync(new() { FromDate = new(2026, 1, 31), ToDate = new(2026, 1, 31), PageSize = 1, PageNumber = 99 }, default);
        Assert.Empty(again.Items); Assert.Equal(3, again.TotalCount);
    }

    [Theory]
    [InlineData("Europe/Istanbul", 2026, 1, 2, false)]
    [InlineData("America/Los_Angeles", 2026, 1, 1, false)]
    [InlineData(null, 2026, 1, 1, true)]
    [InlineData("Not/A_Zone", 2026, 1, 1, true)]
    [InlineData("   ", 2026, 1, 1, true)]
    public async Task Default_today_uses_user_timezone_or_explicit_utc_fallback(string? zone, int year, int month, int day, bool fallback)
    {
        var store = new MemoryStore([]) { Zone = zone };
        var summary = await Service(store, new DateTimeOffset(2026, 1, 1, 22, 30, 0, TimeSpan.Zero)).GetSummaryAsync(new(), default);
        Assert.Equal(new DateOnly(year, month, day), summary.Context.Today);
        Assert.Equal(summary.Context.Today, summary.Context.FromDate);
        Assert.Equal(summary.Context.FromDate.AddDays(30), summary.Context.ToDate);
        Assert.Equal(fallback, summary.Context.UsedTimeZoneFallback);
        if (fallback) Assert.Equal("UTC", summary.Context.TimeZoneId);
    }

    [Fact]
    public async Task Empty_dashboard_and_maximum_date_defaults_are_safe()
    {
        var service = Service(new([]), DateTimeOffset.MaxValue);
        var result = await service.GetSummaryAsync(new(), default);
        Assert.Equal(DateOnly.MaxValue, result.Context.ToDate); Assert.Equal(0, result.ActivePaymentPlanCount);
        Assert.Empty(result.UpcomingTotalsByCurrency); Assert.Empty(result.PaidTotalsByCurrency); Assert.Null(result.NextUpcomingPayment);
        Assert.Equal(0, result.PendingPaymentCount);
        var calendar = await service.GetCalendarAsync(new() { Year = 9999, Month = 12 }, default);
        Assert.Empty(calendar.Days); Assert.Equal(DateOnly.MaxValue, calendar.Context.ToDate);
    }

    [Theory]
    [InlineData("2026-02-01", "2026-01-01")]
    [InlineData("2026-01-01", "2027-01-02")]
    public async Task Invalid_or_oversized_ranges_are_rejected(string from, string to)
        => await Assert.ThrowsAsync<ValidationException>(() => Service(new([])).GetUpcomingAsync(new() { FromDate = DateOnly.Parse(from), ToDate = DateOnly.Parse(to) }, default));

    [Fact]
    public async Task Defaults_are_validated_after_timezone_resolution_and_identity_is_required()
    {
        await Assert.ThrowsAsync<DomainRuleException>(() => Service(new([])).GetSummaryAsync(new() { ToDate = new(2025, 1, 1) }, default));
        var service = Service(new([]), userId: Guid.Empty);
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => service.GetSummaryAsync(new(), default));
    }

    [Theory]
    [InlineData(0, 20, "TRY", OccurrenceStatus.Pending)]
    [InlineData(1, 101, "TRY", OccurrenceStatus.Pending)]
    [InlineData(1, 20, "ZZZ", OccurrenceStatus.Pending)]
    [InlineData(1, 20, "TRY", (OccurrenceStatus)99)]
    public async Task Query_status_currency_and_pagination_are_validated(int page, int size, string currency, OccurrenceStatus status)
        => Assert.False((await new UpcomingPaymentsValidator().ValidateAsync(new UpcomingPaymentsQuery { PageNumber = page, PageSize = size, CurrencyCode = currency, Status = status })).IsValid);

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, 1)]
    [InlineData(10000, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public async Task Calendar_year_month_are_required_and_bounded(int? year, int? month)
        => Assert.False((await new CalendarQueryValidator().ValidateAsync(new CalendarQuery { Year = year, Month = month })).IsValid);

    [Fact]
    public async Task Currency_filter_is_normalized_and_forwarded_with_server_owner()
    {
        var store = new MemoryStore([Plan("USD")]); var owner = Guid.NewGuid();
        var result = await Service(store, userId: owner).GetUpcomingAsync(new() { CurrencyCode = " usd " }, default);
        Assert.Equal("USD", store.Currency); Assert.Equal(owner, store.Owner); Assert.Single(result.Items);
    }

    [Theory]
    [InlineData(10, 30, false)]
    [InlineData(10, 365, false)]
    [InlineData(100, 30, false)]
    [InlineData(100, 365, false)]
    [InlineData(1000, 30, false)]
    [InlineData(1000, 365, true)]
    public async Task Representative_workloads_are_bounded_and_record_local_timings(int planCount, int days, bool limited)
    {
        var plans = Enumerable.Range(0, planCount).Select(_ => { var plan = Plan(); plan.StartDate = new(2026, 1, 1); plan.Frequency = PaymentFrequency.Daily; return plan; }).ToArray();
        var query = new UpcomingPaymentsQuery { FromDate = new(2026, 1, 1), ToDate = new DateOnly(2026, 1, 1).AddDays(days - 1), PageSize = 100 };
        var service = Service(new(plans));
        var thread = Environment.CurrentManagedThreadId;
        var beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        if (limited) await Assert.ThrowsAsync<DomainRuleException>(() => service.GetUpcomingAsync(query, default));
        else { var result = await service.GetUpcomingAsync(query, default); Assert.Equal(planCount * days, result.TotalCount); Assert.Equal(new DateOnly(2026, 1, 1), result.Items[0].DueDate); }
        var allocated = thread == Environment.CurrentManagedThreadId ? (long?)(GC.GetAllocatedBytesForCurrentThread() - beforeAllocation) : null;
        output.WriteLine("{0} plans x {1} days: limited={2}, {3:F3} ms, thread allocation={4} bytes (local Debug)", planCount, days, limited, watch.Elapsed.TotalMilliseconds, allocated);
    }

    [Fact]
    public async Task Plan_limit_is_not_silently_truncated()
        => await Assert.ThrowsAsync<DomainRuleException>(() => Service(new(Enumerable.Range(0, 1001).Select(_ => Plan()).ToArray())).GetSummaryAsync(new(), default));

    private static PaymentPlan Plan(string currency = "TRY", decimal amount = 10, string name = "Payment")
        => new() { Id = Guid.NewGuid(), Name = name, Amount = amount, CurrencyCode = currency, Category = "Bills", StartDate = new(2026, 1, 31), Frequency = PaymentFrequency.Monthly, IsActive = true };
    private static PaymentOccurrence Occurrence(PaymentPlan plan, DateOnly date, OccurrenceStatus status, decimal? amount = null)
        => PaymentOccurrence.Create(plan.Id, date, status, amount, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    private static OverviewService Service(MemoryStore store, DateTimeOffset? instant = null, Guid? userId = null)
        => new(store, new UserContext(userId ?? Guid.NewGuid()), new PaymentRecurrenceCalculator(), new FixedClock(instant ?? new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)), new UpcomingPaymentsValidator(), new SummaryQueryValidator(), new CalendarQueryValidator());
    private sealed class UserContext(Guid id) : ICurrentUser { public Guid? UserId => id; }
    private sealed class FixedClock(DateTimeOffset instant) : TimeProvider { public override DateTimeOffset GetUtcNow() => instant; }
    private sealed class MemoryStore(IReadOnlyList<PaymentPlan> plans, IReadOnlyList<PaymentOccurrence>? occurrences = null) : IOverviewStore
    {
        public IReadOnlyList<PaymentOccurrence> Occurrences { get; } = occurrences ?? [];
        public string? Zone { get; init; } = "UTC";
        public string? Currency { get; private set; }
        public Guid Owner { get; private set; }
        public Task<string?> GetTimeZoneIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(Zone);
        public Task<OverviewSnapshot> LoadAsync(Guid userId, DateOnly from, DateOnly to, string? currencyCode, bool includeActiveCount, CancellationToken cancellationToken)
        {
            Owner = userId; Currency = currencyCode;
            var selected = plans.Where(x => currencyCode is null || x.CurrencyCode == currencyCode).ToArray();
            var ids = selected.Select(x => x.Id).ToHashSet();
            return Task.FromResult(new OverviewSnapshot(selected, Occurrences.Where(x => ids.Contains(x.PaymentPlanId) && x.DueDate >= from && x.DueDate <= to).ToArray(), includeActiveCount ? plans.Count(x => x.IsActive) : 0));
        }
    }
}
