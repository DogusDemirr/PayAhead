using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class RecurrenceIntegrationTests(SqlServerFixture server) : IClassFixture<SqlServerFixture>
{
    [Theory]
    [InlineData(PaymentFrequency.Monthly, 1, "2026-01-31", null, "2026-01-01", "2026-05-31", "2026-01-31,2026-02-28,2026-03-31,2026-04-30,2026-05-31")]
    [InlineData(PaymentFrequency.Monthly, 1, "2026-01-15", 31, "2026-01-15", "2026-03-31", "2026-01-15,2026-02-28,2026-03-31")]
    [InlineData(PaymentFrequency.Yearly, 1, "2024-02-29", null, "2024-01-01", "2028-02-29", "2024-02-29,2025-02-28,2026-02-28,2027-02-28,2028-02-29")]
    [InlineData(PaymentFrequency.Daily, 2, "2024-02-27", null, "2024-02-28", "2024-03-02", "2024-02-29,2024-03-02")]
    [InlineData(PaymentFrequency.Weekly, 2, "2025-12-22", null, "2025-12-22", "2026-01-19", "2025-12-22,2026-01-05,2026-01-19")]
    public async Task Sql_dateonly_plan_roundtrip_matches_domain_calculation_without_writing_occurrences(
        PaymentFrequency frequency, int interval, string start, int? anchor, string from, string to, string expected)
    {
        var builder = new SqlConnectionStringBuilder(server.ConnectionString)
        { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") };
        await using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(builder.ConnectionString).Options);
        try
        {
            await db.Database.MigrateAsync();
            var userId = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = userId, Email = "recurrence@example.invalid", NormalizedEmail = "RECURRENCE@EXAMPLE.INVALID",
                DisplayName = "Recurrence test", PasswordHash = "test-only-placeholder", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow
            });
            var plan = PaymentPlan.Create(userId, new PaymentPlanDetails("Schedule", null, 15, "TRY", "Bills", frequency,
                interval, D(start), D(to), anchor, true), DateTime.UtcNow);
            db.PaymentPlans.Add(plan);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var loaded = await db.PaymentPlans.AsNoTracking().SingleAsync(x => x.Id == plan.Id);
            Assert.Equal(D(start), loaded.StartDate); Assert.Equal(D(to), loaded.EndDate);
            Assert.Equal(anchor, loaded.AnchorDay); Assert.Equal(interval, loaded.Interval); Assert.Equal(frequency, loaded.Frequency);
            using var services = new ServiceCollection().AddApplication().BuildServiceProvider();
            var calculator = services.GetRequiredService<IPaymentRecurrenceCalculator>();
            var dates = expected.Split(',').Select(D).ToArray();
            Assert.Equal(dates, calculator.CalculateOccurrences(loaded, D(from), D(to)));
            Assert.Equal(dates[0], calculator.GetNextOccurrence(loaded, D(from)));
            Assert.Equal(dates[^1], calculator.GetNextOccurrence(loaded, dates[^1]));
            Assert.Null(calculator.GetNextOccurrence(loaded, D(to).AddDays(1)));
            Assert.Equal(D(start), loaded.StartDate); Assert.Equal(anchor, loaded.AnchorDay);
            Assert.Equal(0, await db.PaymentOccurrences.CountAsync());
            Assert.Equal(plan.RowVersion, loaded.RowVersion);
            await db.Database.OpenConnectionAsync();
            try
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('PaymentPlans') AND name IN ('StartDate','EndDate') AND TYPE_NAME(system_type_id) = 'date'";
                Assert.Equal(2, (int)(await command.ExecuteScalarAsync())!);
            }
            finally { await db.Database.CloseConnectionAsync(); }
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != builder.InitialCatalog
                || !builder.InitialCatalog.StartsWith("PayAheadTest_", StringComparison.Ordinal))
                throw new InvalidOperationException("Test veritabanı güvenlik kontrolü başarısız.");
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static DateOnly D(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
