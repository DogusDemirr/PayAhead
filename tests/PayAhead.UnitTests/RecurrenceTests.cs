using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using Xunit;
using Xunit.Abstractions;

namespace PayAhead.UnitTests;

public sealed class RecurrenceTests(ITestOutputHelper output)
{
    private readonly IPaymentRecurrenceCalculator calculator = new PaymentRecurrenceCalculator();

    public static IEnumerable<object?[]> Schedules()
    {
        yield return Case(PaymentFrequency.Once, 1, "2026-01-31", "2026-01-01", "2026-12-31", "2026-01-31");
        yield return Case(PaymentFrequency.Once, 3, "2026-01-31", "2026-01-31", "2026-01-31", "2026-01-31", 15);
        yield return Case(PaymentFrequency.Once, 1, "2026-01-31", "2026-02-01", "2026-12-31", "");
        yield return Case(PaymentFrequency.Once, 1, "2026-01-31", "2026-01-01", "2026-01-30", "");
        yield return Case(PaymentFrequency.Daily, 1, "2026-01-01", "2025-12-30", "2026-01-03", "2026-01-01,2026-01-02,2026-01-03");
        yield return Case(PaymentFrequency.Daily, 3, "2026-01-01", "2026-01-01", "2026-01-10", "2026-01-01,2026-01-04,2026-01-07,2026-01-10");
        yield return Case(PaymentFrequency.Daily, 2, "2026-01-01", "2026-01-02", "2026-01-06", "2026-01-03,2026-01-05");
        yield return Case(PaymentFrequency.Weekly, 2, "2026-01-05", "2026-01-05", "2026-02-02", "2026-01-05,2026-01-19,2026-02-02");
        yield return Case(PaymentFrequency.Weekly, 3, "2025-12-22", "2025-12-22", "2026-02-02", "2025-12-22,2026-01-12,2026-02-02");
        yield return Case(PaymentFrequency.Monthly, 1, "2026-01-31", "2026-01-01", "2026-05-31", "2026-01-31,2026-02-28,2026-03-31,2026-04-30,2026-05-31");
        yield return Case(PaymentFrequency.Monthly, 1, "2026-01-30", "2026-01-30", "2026-05-30", "2026-01-30,2026-02-28,2026-03-30,2026-04-30,2026-05-30");
        yield return Case(PaymentFrequency.Monthly, 1, "2024-01-31", "2024-01-31", "2024-03-31", "2024-01-31,2024-02-29,2024-03-31");
        yield return Case(PaymentFrequency.Monthly, 1, "2025-11-30", "2025-11-30", "2026-03-30", "2025-11-30,2025-12-30,2026-01-30,2026-02-28,2026-03-30");
        yield return Case(PaymentFrequency.Monthly, 2, "2026-01-31", "2026-01-31", "2026-07-31", "2026-01-31,2026-03-31,2026-05-31,2026-07-31");
        yield return Case(PaymentFrequency.Monthly, 3, "2025-11-30", "2026-01-01", "2026-08-30", "2026-02-28,2026-05-30,2026-08-30");
        yield return Case(PaymentFrequency.Monthly, 1, "2026-01-15", "2026-01-01", "2026-04-30", "2026-01-15,2026-02-28,2026-03-31,2026-04-30", 31);
        yield return Case(PaymentFrequency.Monthly, 1, "2026-01-31", "2026-01-01", "2026-03-31", "2026-01-31,2026-02-15,2026-03-15", 15);
        yield return Case(PaymentFrequency.Monthly, 1, "2026-04-30", "2026-04-30", "2026-06-30", "2026-04-30,2026-05-30,2026-06-30");
        yield return Case(PaymentFrequency.Monthly, 1, "2026-04-30", "2026-04-30", "2026-06-30", "2026-04-30,2026-05-31,2026-06-30", 31);
        yield return Case(PaymentFrequency.Monthly, 1, "2026-01-31", "2026-02-28", "2026-02-28", "2026-02-28");
        yield return Case(PaymentFrequency.Monthly, 1, "2026-01-31", "2026-03-01", "2026-03-30", "");
        yield return Case(PaymentFrequency.Yearly, 1, "2024-02-29", "2024-01-01", "2028-12-31", "2024-02-29,2025-02-28,2026-02-28,2027-02-28,2028-02-29");
        yield return Case(PaymentFrequency.Yearly, 2, "2024-02-29", "2024-02-29", "2028-02-29", "2024-02-29,2026-02-28,2028-02-29");
        yield return Case(PaymentFrequency.Yearly, 3, "2024-02-29", "2025-01-01", "2030-12-31", "2027-02-28,2030-02-28");
        yield return Case(PaymentFrequency.Yearly, 1, "2096-02-29", "2099-01-01", "2104-12-31", "2099-02-28,2100-02-28,2101-02-28,2102-02-28,2103-02-28,2104-02-29");
        yield return Case(PaymentFrequency.Yearly, 1, "2024-06-15", "2024-06-15", "2026-12-31", "2024-06-15,2025-06-30,2026-06-30", 31);
    }

    [Theory]
    [MemberData(nameof(Schedules))]
    public void Dates_match_explicit_schedule_and_are_unique_ordered_and_deterministic(PaymentFrequency frequency,
        int interval, string start, string from, string to, string expected, int? anchor)
    {
        var plan = Plan(frequency, start, interval, anchor);
        var dates = calculator.CalculateOccurrences(plan, D(from), D(to));
        Assert.Equal(Dates(expected), dates);
        Assert.Equal(dates.OrderBy(x => x).Distinct(), dates);
        Assert.Equal(dates, calculator.CalculateOccurrences(plan, D(from), D(to)));
        Assert.Equal(D(start), plan.StartDate); Assert.Equal(anchor, plan.AnchorDay);
    }

    [Theory]
    [InlineData("2025-12-01", "2026-01-31")]
    [InlineData("2026-01-31", "2026-01-31")]
    [InlineData("2026-02-01", "2026-02-28")]
    [InlineData("2026-02-28", "2026-02-28")]
    [InlineData("2026-03-01", "2026-03-31")]
    [InlineData("2026-03-31", "2026-03-31")]
    [InlineData("2026-04-01", "2026-04-30")]
    public void Next_is_inclusive_and_keeps_original_monthly_anchor(string reference, string expected)
        => Assert.Equal(D(expected), calculator.GetNextOccurrence(Plan(PaymentFrequency.Monthly), D(reference)));

    [Theory]
    [InlineData(PaymentFrequency.Once, "2026-02-01", null)]
    [InlineData(PaymentFrequency.Daily, "2026-02-01", "2026-02-02")]
    [InlineData(PaymentFrequency.Weekly, "2026-02-01", "2026-02-14")]
    [InlineData(PaymentFrequency.Monthly, "2026-03-01", "2026-03-31")]
    [InlineData(PaymentFrequency.Yearly, "2027-01-31", "2028-01-31")]
    public void Next_uses_interval_without_enumerating_past_dates(PaymentFrequency frequency, string reference, string? expected)
        => Assert.Equal(expected is null ? (DateOnly?)null : D(expected), calculator.GetNextOccurrence(Plan(frequency, interval: 2), D(reference)));

    [Theory]
    [InlineData("2025-02-01", "2025-02-28")]
    [InlineData("2025-02-28", "2025-02-28")]
    [InlineData("2025-03-01", "2026-02-28")]
    [InlineData("2028-02-28", "2028-02-29")]
    [InlineData("2100-02-28", "2100-02-28")]
    [InlineData("2100-03-01", "2101-02-28")]
    public void Next_yearly_preserves_february_29_across_leap_and_century_boundaries(string reference, string expected)
        => Assert.Equal(D(expected), calculator.GetNextOccurrence(Plan(PaymentFrequency.Yearly, "2024-02-29"), D(reference)));

    [Fact]
    public void Explicit_anchor_does_not_create_a_second_payment_in_the_start_month()
    {
        var plan = Plan(PaymentFrequency.Monthly, "2026-01-15", anchor: 31);
        Assert.Equal(D("2026-02-28"), calculator.GetNextOccurrence(plan, D("2026-01-16")));
        Assert.Equal(Dates("2026-02-28,2026-03-31"), calculator.CalculateOccurrences(plan, D("2026-01-16"), D("2026-03-31")));
    }

    [Theory]
    [InlineData(PaymentFrequency.Daily, "2026-01-05,2026-01-07,2026-01-09")]
    [InlineData(PaymentFrequency.Weekly, "2026-01-05,2026-01-19,2026-02-02")]
    public void Daily_and_weekly_ignore_valid_monthly_anchor(PaymentFrequency frequency, string expected)
    {
        var plan = Plan(frequency, "2026-01-05", 2, 31);
        var dates = Dates(expected);
        Assert.Equal(dates, calculator.CalculateOccurrences(plan, dates[0], dates[^1]));
    }

    [Theory]
    [InlineData(PaymentFrequency.Once)]
    [InlineData(PaymentFrequency.Daily)]
    [InlineData(PaymentFrequency.Weekly)]
    [InlineData(PaymentFrequency.Monthly)]
    [InlineData(PaymentFrequency.Yearly)]
    public void End_date_and_start_date_are_both_inclusive(PaymentFrequency frequency)
    {
        var plan = Plan(frequency); plan.EndDate = plan.StartDate;
        Assert.Equal(new[] { D("2026-01-31") }, calculator.CalculateOccurrences(plan, D("2025-01-01"), D("2030-01-01")));
        Assert.Equal(D("2026-01-31"), calculator.GetNextOccurrence(plan, D("2026-01-31")));
        Assert.Null(calculator.GetNextOccurrence(plan, D("2026-02-01")));
    }

    [Fact]
    public void End_date_between_occurrences_clips_without_creating_a_payment()
    {
        var plan = Plan(PaymentFrequency.Monthly); plan.EndDate = D("2026-03-30");
        Assert.Equal(Dates("2026-01-31,2026-02-28"), calculator.CalculateOccurrences(plan, D("2026-01-01"), D("2027-12-31")));
        Assert.Null(calculator.GetNextOccurrence(plan, D("2026-03-01")));
        Assert.Empty(calculator.CalculateOccurrences(plan, D("2027-01-01"), D("2027-12-31")));
    }

    [Fact]
    public void Inactive_plan_produces_no_dates_including_at_its_start()
    {
        var plan = Plan(PaymentFrequency.Monthly); plan.IsActive = false;
        Assert.Equal(Array.Empty<DateOnly>(), calculator.CalculateOccurrences(plan, plan.StartDate, D("2030-12-31")));
        Assert.Null(calculator.GetNextOccurrence(plan, plan.StartDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Invalid_intervals_fail_both_methods_even_for_inactive_plans(int interval)
    {
        var plan = Plan(PaymentFrequency.Daily, interval: interval); plan.IsActive = false;
        Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, plan.StartDate, DateOnly.MaxValue));
        Assert.Throws<DomainRuleException>(() => calculator.GetNextOccurrence(plan, plan.StartDate));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Invalid_frequency_fails_both_methods(int frequency)
    {
        var plan = Plan((PaymentFrequency)frequency);
        Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, plan.StartDate, DateOnly.MaxValue));
        Assert.Throws<DomainRuleException>(() => calculator.GetNextOccurrence(plan, plan.StartDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Invalid_anchor_fails_both_methods(int anchor)
    {
        var plan = Plan(PaymentFrequency.Monthly, anchor: anchor);
        Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, plan.StartDate, DateOnly.MaxValue));
        Assert.Throws<DomainRuleException>(() => calculator.GetNextOccurrence(plan, plan.StartDate));
    }

    [Fact]
    public void Invalid_range_and_plan_end_date_are_explicit_errors()
    {
        var plan = Plan(PaymentFrequency.Once);
        Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, D("2026-02-01"), D("2026-01-01")));
        plan.EndDate = plan.StartDate.AddDays(-1);
        Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, plan.StartDate, DateOnly.MaxValue));
        Assert.Throws<DomainRuleException>(() => calculator.GetNextOccurrence(plan, plan.StartDate));
        Assert.Throws<ArgumentNullException>(() => calculator.GetNextOccurrence(null!, DateOnly.MinValue));
        Assert.Throws<ArgumentNullException>(() => calculator.CalculateOccurrences(null!, DateOnly.MinValue, DateOnly.MaxValue));
    }

    [Theory]
    [InlineData(PaymentFrequency.Once)]
    [InlineData(PaymentFrequency.Daily)]
    [InlineData(PaymentFrequency.Weekly)]
    [InlineData(PaymentFrequency.Monthly)]
    [InlineData(PaymentFrequency.Yearly)]
    public void Maximum_date_is_a_valid_final_occurrence_without_overflow(PaymentFrequency frequency)
    {
        var plan = Plan(frequency, "9999-12-31");
        Assert.Equal(new[] { DateOnly.MaxValue }, calculator.CalculateOccurrences(plan, DateOnly.MinValue, DateOnly.MaxValue));
        Assert.Equal(DateOnly.MaxValue, calculator.GetNextOccurrence(plan, DateOnly.MaxValue));
    }

    [Theory]
    [InlineData(PaymentFrequency.Once)]
    [InlineData(PaymentFrequency.Daily)]
    [InlineData(PaymentFrequency.Weekly)]
    [InlineData(PaymentFrequency.Monthly)]
    [InlineData(PaymentFrequency.Yearly)]
    public void Maximum_interval_never_overflows_or_loops(PaymentFrequency frequency)
    {
        var plan = Plan(frequency, "0001-01-01", int.MaxValue);
        Assert.Equal(new[] { DateOnly.MinValue }, calculator.CalculateOccurrences(plan, DateOnly.MinValue, DateOnly.MaxValue));
        Assert.Equal(DateOnly.MinValue, calculator.GetNextOccurrence(plan, DateOnly.MinValue));
        Assert.Null(calculator.GetNextOccurrence(plan, DateOnly.MinValue.AddDays(1)));
    }

    [Fact]
    public void Minimum_date_and_late_range_do_not_require_scanning_previous_millions_of_days()
    {
        var plan = Plan(PaymentFrequency.Daily, "0001-01-01");
        Assert.Equal(Dates("0001-01-01,0001-01-02,0001-01-03"), calculator.CalculateOccurrences(plan, DateOnly.MinValue, D("0001-01-03")));
        Assert.Equal(Dates("9999-12-29,9999-12-30,9999-12-31"), calculator.CalculateOccurrences(plan, D("9999-12-29"), DateOnly.MaxValue));
        Assert.Equal(DateOnly.MaxValue, calculator.GetNextOccurrence(plan, DateOnly.MaxValue));
    }

    [Fact]
    public void Broad_sparse_range_is_allowed_when_it_fits_the_result_limit()
    {
        var plan = Plan(PaymentFrequency.Yearly, "0001-01-01");
        var dates = calculator.CalculateOccurrences(plan, DateOnly.MinValue, DateOnly.MaxValue);
        Assert.Equal(Enumerable.Range(1, 9999).Select(year => new DateOnly(year, 1, 1)), dates);
        Assert.Equal(DateOnly.MinValue, dates[0]);
        Assert.Equal(D("9999-01-01"), dates[^1]);
    }

    [Fact]
    public void Period_boundaries_near_maximum_date_are_safe()
    {
        var weekly = Plan(PaymentFrequency.Weekly, "9999-12-24");
        Assert.Equal(Dates("9999-12-24,9999-12-31"), calculator.CalculateOccurrences(weekly, D("9999-12-01"), DateOnly.MaxValue));
        var monthly = Plan(PaymentFrequency.Monthly, "9999-11-30", anchor: 31);
        Assert.Equal(Dates("9999-11-30,9999-12-31"), calculator.CalculateOccurrences(monthly, D("9999-11-01"), DateOnly.MaxValue));
        var yearly = Plan(PaymentFrequency.Yearly, "9998-02-28", anchor: 29);
        Assert.Equal(Dates("9998-02-28,9999-02-28"), calculator.CalculateOccurrences(yearly, D("9998-01-01"), DateOnly.MaxValue));
        Assert.Null(calculator.GetNextOccurrence(yearly, D("9999-03-01")));
    }

    [Fact]
    public void Result_limit_is_inclusive_and_excess_is_an_error_not_a_partial_list()
    {
        var plan = Plan(PaymentFrequency.Daily, "2000-01-01");
        var last = plan.StartDate.AddDays(PaymentRecurrenceCalculator.MaximumOccurrences - 1);
        var dates = calculator.CalculateOccurrences(plan, plan.StartDate, last);
        Assert.Equal(Enumerable.Range(0, 10_000).Select(plan.StartDate.AddDays), dates);
        var error = Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, plan.StartDate, last.AddDays(1)));
        Assert.Contains("10000", error.Message);
        Assert.Throws<DomainRuleException>(() => calculator.CalculateOccurrences(plan, DateOnly.MinValue, DateOnly.MaxValue));
        Assert.Equal(DateOnly.MaxValue, calculator.GetNextOccurrence(plan, DateOnly.MaxValue));
    }

    [Fact]
    public void Application_can_resolve_shared_stateless_domain_service()
    {
        using var services = new ServiceCollection().AddApplication().BuildServiceProvider();
        var resolved = services.GetRequiredService<IPaymentRecurrenceCalculator>();
        Assert.Same(resolved, services.GetRequiredService<IPaymentRecurrenceCalculator>());
        Assert.Equal(D("2026-02-28"), resolved.GetNextOccurrence(Plan(PaymentFrequency.Monthly), D("2026-02-01")));
    }

    [Theory]
    [InlineData(PaymentFrequency.Monthly, "2026-01-01", "2035-12-31", 120, "2035-12-01")]
    [InlineData(PaymentFrequency.Yearly, "2026-01-01", "2055-12-31", 30, "2055-01-01")]
    [InlineData(PaymentFrequency.Daily, "2026-01-01", "2030-12-31", 1826, "2030-12-31")]
    public void Representative_workloads_return_exact_dates_and_record_local_timings(PaymentFrequency frequency,
        string start, string end, int expectedCount, string expectedLast)
    {
        var plan = Plan(frequency, start);
        var first = D(start); var last = D(end);
        var expected = Enumerable.Range(0, expectedCount).Select(index => frequency switch
        {
            PaymentFrequency.Monthly => first.AddMonths(index),
            PaymentFrequency.Yearly => first.AddYears(index),
            _ => first.AddDays(index)
        }).ToArray();
        Assert.Equal(expected, calculator.CalculateOccurrences(plan, first, last));
        Assert.Equal(D(expectedLast), expected[^1]);
        var timer = Stopwatch.StartNew();
        for (var run = 0; run < 100; run++) calculator.CalculateOccurrences(plan, first, last);
        timer.Stop();
        output.WriteLine("{0}: {1} dates, 100 runs {2:F3} ms, average {3:F4} ms", frequency,
            expectedCount, timer.Elapsed.TotalMilliseconds, timer.Elapsed.TotalMilliseconds / 100);
    }

    private static object?[] Case(PaymentFrequency frequency, int interval, string start, string from, string to, string expected, int? anchor = null)
        => [frequency, interval, start, from, to, expected, anchor];
    private static DateOnly D(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateOnly[] Dates(string values) => values.Length == 0 ? [] : values.Split(',').Select(D).ToArray();
    private static PaymentPlan Plan(PaymentFrequency frequency, string start = "2026-01-31", int interval = 1, int? anchor = null)
        => new() { StartDate = D(start), Frequency = frequency, Interval = interval, AnchorDay = anchor, IsActive = true };
}
