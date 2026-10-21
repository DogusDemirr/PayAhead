using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application;
using PayAhead.Application.Overview;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class OverviewApiTests : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private readonly string connection;
    private readonly PayAheadDbContext db;
    private readonly Guid owner = Guid.NewGuid();
    private readonly Guid other = Guid.NewGuid();
    private readonly List<WebApplicationFactory<Program>> factories = [];
    private static readonly JsonSerializerOptions Json = CreateJson();
    private static readonly DateTime Now = new(2026, 1, 1, 22, 30, 0, DateTimeKind.Utc);
    private PaymentPlan tryPlan = null!;
    private PaymentPlan usdPlan = null!;
    private PaymentPlan inactive = null!;

    public OverviewApiTests(SqlServerFixture server)
    {
        connection = new SqlConnectionStringBuilder(server.ConnectionString)
        { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        db = new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    }
    public async Task InitializeAsync()
    {
        await db.Database.MigrateAsync();
        foreach (var id in new[] { owner, other })
        {
            db.Users.Add(new() { Id = id, Email = $"{id:N}@example.invalid", NormalizedEmail = $"{id:N}@EXAMPLE.INVALID", DisplayName = "Overview", PasswordHash = "test-only-placeholder", CreatedAtUtc = Now, UpdatedAtUtc = Now });
            db.UserSettings.Add(new() { UserId = id, TimeZoneId = id == owner ? "Europe/Istanbul" : "UTC", CreatedAtUtc = Now, UpdatedAtUtc = Now });
        }
        tryPlan = Plan(owner, "TRY", 10.1234m, "A TRY"); usdPlan = Plan(owner, "USD", 2.5m, "B USD");
        inactive = Plan(owner, "EUR", 100, "C inactive"); inactive.IsActive = false;
        var secret = Plan(other, "USD", 999, "Other private");
        db.PaymentPlans.AddRange(tryPlan, usdPlan, inactive, secret); await db.SaveChangesAsync();
        db.PaymentOccurrences.AddRange(
            Occurrence(tryPlan, new(2026, 1, 31), OccurrenceStatus.Paid, 7.4321m),
            Occurrence(usdPlan, new(2026, 1, 31), OccurrenceStatus.Skipped),
            Occurrence(inactive, new(2026, 1, 15), OccurrenceStatus.Paid, 88.8888m),
            Occurrence(inactive, new(2026, 2, 15), OccurrenceStatus.Skipped),
            Occurrence(secret, new(2026, 1, 31), OccurrenceStatus.Paid, 250));
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    }
    public async Task DisposeAsync()
    {
        foreach (var factory in factories) await factory.DisposeAsync();
        if (!db.Database.GetDbConnection().Database.StartsWith("PayAheadTest_", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe test cleanup");
        await db.Database.EnsureDeletedAsync(); await db.DisposeAsync();
    }

    [Fact]
    public async Task Upcoming_merges_statuses_precision_pagination_and_does_not_write_occurrences()
    {
        using var client = Client(owner);
        const string range = "fromDate=2026-01-01&toDate=2026-03-31";
        var pending = await client.GetFromJsonAsync<UpcomingPaymentsResponse>("/api/payments/upcoming?" + range + "&pageSize=1", Json);
        Assert.Equal(4, pending!.TotalCount); Assert.Equal(4, pending.TotalPages);
        Assert.Equal(tryPlan.Id, Assert.Single(pending.Items).PaymentPlanId); Assert.Equal(new DateOnly(2026, 2, 28), pending.Items[0].DueDate);
        Assert.Equal(10.1234m, pending.Items[0].Amount); Assert.Equal(OccurrenceStatus.Pending, pending.Items[0].Status);
        var second = await client.GetFromJsonAsync<UpcomingPaymentsResponse>("/api/payments/upcoming?" + range + "&pageSize=1&pageNumber=2", Json);
        Assert.Equal(usdPlan.Id, Assert.Single(second!.Items).PaymentPlanId);
        var usd = await client.GetFromJsonAsync<UpcomingPaymentsResponse>("/api/payments/upcoming?" + range + "&currencyCode=%20usd%20", Json);
        Assert.Equal(2, usd!.TotalCount); Assert.All(usd.Items, x => Assert.Equal("USD", x.CurrencyCode));
        var paid = await client.GetFromJsonAsync<UpcomingPaymentsResponse>("/api/payments/upcoming?" + range + "&status=Paid", Json);
        Assert.Equal(2, paid!.TotalCount); Assert.Equal(88.8888m, paid.Items[0].Amount); Assert.Equal(7.4321m, paid.Items[1].Amount);
        Assert.All(paid.Items, x => Assert.Equal(DateTimeKind.Utc, x.PaidAtUtc!.Value.Kind));
        var skipped = await client.GetFromJsonAsync<UpcomingPaymentsResponse>("/api/payments/upcoming?" + range + "&status=Skipped", Json);
        Assert.Equal(2, skipped!.TotalCount);
        Assert.Equal(5, await db.PaymentOccurrences.CountAsync());
    }

    [Fact]
    public async Task Dashboard_groups_currencies_uses_actual_paid_values_and_counts_active_flags()
    {
        using var client = Client(owner);
        var summary = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary?fromDate=2026-01-01&toDate=2026-02-28", Json);
        Assert.Equal(2, summary!.ActivePaymentPlanCount); Assert.Equal(2, summary.UpcomingPaymentCount);
        Assert.Equal(2, summary.PendingPaymentCount); Assert.Equal(2, summary.PaidPaymentCount); Assert.Equal(2, summary.SkippedPaymentCount);
        Assert.Equal(new[] { new CurrencyTotal("TRY", 10.1234m), new CurrencyTotal("USD", 2.5m) }, summary.UpcomingTotalsByCurrency);
        Assert.Equal(new[] { new CurrencyTotal("EUR", 88.8888m), new CurrencyTotal("TRY", 7.4321m) }, summary.PaidTotalsByCurrency);
        Assert.Equal(tryPlan.Id, summary.NextUpcomingPayment!.PaymentPlanId);
        var seven = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary?fromDate=2026-01-28&toDate=2026-02-03", Json);
        Assert.Equal(0, seven!.PendingPaymentCount); Assert.Equal(1, seven.PaidPaymentCount); Assert.Equal(1, seven.SkippedPaymentCount);
    }

    [Fact]
    public async Task Calendar_returns_only_requested_month_with_inactive_history_and_pending_day_totals()
    {
        using var client = Client(owner);
        var january = await client.GetFromJsonAsync<CalendarResponse>("/api/calendar?year=2026&month=1", Json);
        Assert.Equal(new[] { new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 31) }, january!.Days.Select(x => x.Date));
        Assert.Equal(2, january.Days[1].PaymentCount); Assert.All(january.Days, x => Assert.Empty(x.TotalsByCurrency));
        var february = await client.GetFromJsonAsync<CalendarResponse>("/api/calendar?year=2026&month=2", Json);
        Assert.Equal(new[] { new DateOnly(2026, 2, 15), new DateOnly(2026, 2, 28) }, february!.Days.Select(x => x.Date));
        Assert.Equal(new[] { new CurrencyTotal("TRY", 10.1234m), new CurrencyTotal("USD", 2.5m) }, february.Days[1].TotalsByCurrency);
        var usd = await client.GetFromJsonAsync<CalendarResponse>("/api/calendar?year=2026&month=2&status=Pending&currencyCode=USD", Json);
        Assert.Equal(usdPlan.Id, Assert.Single(Assert.Single(usd!.Days).Payments).PaymentPlanId);
    }

    [Fact]
    public async Task Current_timezone_drives_defaults_and_invalid_setting_falls_back_to_utc()
    {
        using var client = Client(owner);
        var defaults = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary", Json);
        Assert.Equal(new DateOnly(2026, 1, 2), defaults!.Context.Today); Assert.False(defaults.Context.UsedTimeZoneFallback);
        Assert.Equal(new DateOnly(2026, 2, 1), defaults.Context.ToDate);
        await db.UserSettings.Where(x => x.UserId == owner).ExecuteUpdateAsync(set => set.SetProperty(x => x.TimeZoneId, "Unknown/Zone"));
        var fallback = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary", Json);
        Assert.Equal(new DateOnly(2026, 1, 1), fallback!.Context.Today); Assert.True(fallback.Context.UsedTimeZoneFallback); Assert.Equal("UTC", fallback.Context.TimeZoneId);
        await db.UserSettings.Where(x => x.UserId == owner).ExecuteDeleteAsync();
        Assert.True((await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary", Json))!.Context.UsedTimeZoneFallback);
    }

    [Fact]
    public async Task Different_users_and_query_identity_cannot_expose_other_users_data()
    {
        using var client = Client(owner); using var second = Client(other); using var anonymous = Client(owner, false);
        foreach (var url in new[] { "/api/payments/upcoming?status=Paid&fromDate=2026-01-01&toDate=2026-02-28", "/api/dashboard/summary?fromDate=2026-01-01&toDate=2026-02-28", "/api/calendar?year=2026&month=1" })
        {
            var content = await client.GetStringAsync(url + "&userId=" + other);
            Assert.DoesNotContain("Other private", content); Assert.DoesNotContain("250", content);
            await Problem(await anonymous.GetAsync(url), HttpStatusCode.Unauthorized);
        }
        var otherPaid = await second.GetFromJsonAsync<UpcomingPaymentsResponse>("/api/payments/upcoming?status=Paid&fromDate=2026-01-01&toDate=2026-02-28", Json);
        Assert.Equal("Other private", Assert.Single(otherPaid!.Items).PaymentName);
    }

    [Theory]
    [InlineData("/api/payments/upcoming?fromDate=2026-02-01&toDate=2026-01-01")]
    [InlineData("/api/payments/upcoming?fromDate=2026-01-01&toDate=2027-01-02")]
    [InlineData("/api/payments/upcoming?currencyCode=ZZZ")]
    [InlineData("/api/payments/upcoming?status=999")]
    [InlineData("/api/payments/upcoming?pageNumber=0")]
    [InlineData("/api/payments/upcoming?pageSize=101")]
    [InlineData("/api/payments/upcoming?fromDate=bad")]
    [InlineData("/api/dashboard/summary?fromDate=2026-02-01&toDate=2026-01-01")]
    [InlineData("/api/calendar?year=0&month=1")]
    [InlineData("/api/calendar?year=10000&month=1")]
    [InlineData("/api/calendar?year=2026&month=13")]
    [InlineData("/api/calendar?year=2026&month=0")]
    [InlineData("/api/calendar?year=2026&month=1&status=Invalid")]
    [InlineData("/api/calendar?year=2026&month=1&currencyCode=ZZZ")]
    [InlineData("/api/calendar")]
    public async Task Invalid_queries_are_consistent_problem_details(string url)
    {
        using var client = Client(owner); await Problem(await client.GetAsync(url), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Inactive_saved_history_survives_dates_outside_current_schedule_and_no_pending_is_generated()
    {
        await db.PaymentPlans.Where(x => x.UserId == owner).ExecuteUpdateAsync(set => set.SetProperty(x => x.IsActive, false).SetProperty(x => x.StartDate, new DateOnly(2026, 3, 1)));
        using var client = Client(owner);
        var summary = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary?fromDate=2026-01-01&toDate=2026-02-28", Json);
        Assert.Equal(0, summary!.ActivePaymentPlanCount); Assert.Equal(0, summary.PendingPaymentCount); Assert.Equal(2, summary.PaidPaymentCount);
        Assert.Equal(2, summary.SkippedPaymentCount); Assert.Null(summary.NextUpcomingPayment);
    }

    [Fact]
    public async Task Empty_user_and_dateonly_maximum_month_return_empty_structures()
    {
        using var empty = Client(Guid.NewGuid());
        var summary = await empty.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary", Json);
        Assert.Equal(0, summary!.ActivePaymentPlanCount); Assert.Empty(summary.PaidTotalsByCurrency); Assert.Null(summary.NextUpcomingPayment);
        using var client = Client(owner);
        var calendar = await client.GetFromJsonAsync<CalendarResponse>("/api/calendar?year=9999&month=12", Json);
        Assert.Equal(DateOnly.MaxValue, calendar!.Context.ToDate);
        Assert.All(calendar.Days, x => Assert.Equal(9999, x.Date.Year));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task Bulk_read_command_count_is_constant_for_10_100_1000_plans(int count)
    {
        await AddDailyPlans(count - 3);
        var counter = new CommandCounter();
        await using var measured = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).AddInterceptors(counter).Options);
        var service = new OverviewService(new OverviewStore(measured), new UserContext(owner), new PaymentRecurrenceCalculator(), new FixedClock(),
            new UpcomingPaymentsValidator(), new SummaryQueryValidator(), new CalendarQueryValidator());
        var result = await service.GetUpcomingAsync(new() { FromDate = new(2026, 1, 1), ToDate = new(2026, 1, 31), PageSize = 100 }, default);
        Assert.Equal((count - 3) * 31, result.TotalCount); Assert.Equal(3, counter.Count);
        var summary = await service.GetSummaryAsync(new() { FromDate = new(2026, 1, 1), ToDate = new(2026, 1, 31) }, default);
        Assert.Equal(count - 1, summary.ActivePaymentPlanCount); Assert.Equal(7, counter.Count); // Dashboard için dört ek query.
    }

    [Fact]
    public async Task Combined_result_limit_is_enforced_before_pagination()
    {
        await AddDailyPlans(200);
        using var client = Client(owner);
        await Problem(await client.GetAsync("/api/payments/upcoming?fromDate=2026-01-01&toDate=2026-12-31&pageSize=1"), HttpStatusCode.BadRequest);
        Assert.Equal(5, await db.PaymentOccurrences.CountAsync());
    }

    [Fact]
    public async Task Plan_limit_is_enforced_in_sql_read_without_silent_partial_response()
    {
        await AddDailyPlans(998);
        using var client = Client(owner);
        await Problem(await client.GetAsync("/api/calendar?year=2026&month=1"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Swagger_has_three_overview_paths_and_overdue_pending_is_not_upcoming()
    {
        using var client = Client(owner);
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        foreach (var path in new[] { "/api/payments/upcoming", "/api/dashboard/summary", "/api/calendar" })
            Assert.True(doc.RootElement.GetProperty("paths").GetProperty(path).TryGetProperty("get", out _));
        var daily = Plan(owner, "TRY", 1, "Overdue"); daily.Frequency = PaymentFrequency.Daily; daily.StartDate = new(2026, 1, 1); daily.EndDate = new(2026, 1, 7);
        db.PaymentPlans.Add(daily); await db.SaveChangesAsync();
        var summary = await client.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary?fromDate=2026-01-01&toDate=2026-01-07", Json);
        Assert.Equal(7, summary!.PendingPaymentCount); Assert.Equal(6, summary.UpcomingPaymentCount); Assert.Equal(new DateOnly(2026, 1, 2), summary.NextUpcomingPayment!.DueDate);
    }

    private async Task AddDailyPlans(int count)
    {
        for (var i = 0; i < count; i++) { var extra = Plan(owner, "TRY", 1, $"Daily {i:D4}"); extra.StartDate = new(2026, 1, 1); extra.Frequency = PaymentFrequency.Daily; db.PaymentPlans.Add(extra); }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    }
    private static PaymentPlan Plan(Guid user, string currency, decimal amount, string name)
        => PaymentPlan.Create(user, new(name, null, amount, currency, "Bills", PaymentFrequency.Monthly, 1, new(2026, 1, 31), null, null, true), Now);
    private static PaymentOccurrence Occurrence(PaymentPlan plan, DateOnly date, OccurrenceStatus status, decimal? amount = null)
        => PaymentOccurrence.Create(plan.Id, date, status, amount, Now);
    private HttpClient Client(Guid id, bool authenticated = true)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development").UseRegressionPremium().UseTestIdentity()
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", authenticated.ToString())
            .UseSetting("DevelopmentIdentity:UserId", id.ToString()).ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedClock())));
        factories.Add(factory); return factory.CreateClient();
    }
    private static async Task Problem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32()); Assert.True(document.RootElement.TryGetProperty("traceId", out _));
    }
    private sealed class FixedClock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private sealed class UserContext(Guid id) : ICurrentUser { public Guid? UserId => id; }
    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
    }
    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<PaymentFrequency>(allowIntegerValues: false));
        options.Converters.Add(new JsonStringEnumConverter<OccurrenceStatus>(allowIntegerValues: false));
        return options;
    }
}
