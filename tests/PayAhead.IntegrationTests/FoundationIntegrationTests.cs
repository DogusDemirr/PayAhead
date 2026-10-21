using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class FoundationIntegrationTests(SqlServerFixture sqlServer) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Api_starts_and_liveness_is_public()
    {
        await using var factory = CreateFactory("Server=localhost;Database=Unused;Integrated Security=True;Connect Timeout=1");
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
        var swagger = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/health/live", swagger);
        Assert.Contains("/health/ready", swagger);
        var missing = await client.GetAsync("/missing");
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Migration_constraints_transactions_and_readiness_work_on_real_sql_server()
    {
        var baseConnection = sqlServer.ConnectionString;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N")
        };
        await using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(builder.ConnectionString).Options);
        try
        {
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            await VerifySchemaAsync(db);
            var user = new User { Id = Guid.NewGuid(), Email = "test@example.invalid", NormalizedEmail = "TEST@EXAMPLE.INVALID", DisplayName = "Test", PasswordHash = "test-only-placeholder", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var plan = new PaymentPlan { Id = Guid.NewGuid(), UserId = user.Id, Name = "Test", Amount = 12.3456m, StartDate = new DateOnly(2026, 1, 31), Frequency = PaymentFrequency.Monthly, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            db.PaymentPlans.Add(plan);
            await db.SaveChangesAsync();
            Assert.NotEmpty(plan.RowVersion);
            db.ChangeTracker.Clear();
            var loaded = await db.PaymentPlans.SingleAsync();
            Assert.Equal(12.3456m, loaded.Amount);
            Assert.Equal(new DateOnly(2026, 1, 31), loaded.StartDate);

            await using (var second = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(builder.ConnectionString).Options))
            {
                var stale = await second.PaymentPlans.SingleAsync();
                loaded.Name = "Updated"; await db.SaveChangesAsync();
                Assert.Equal("Updated", await second.PaymentPlans.AsNoTracking().Select(x => x.Name).SingleAsync());
                stale.Name = "Stale";
                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
            }

            db.PaymentOccurrences.Add(new() { Id = Guid.NewGuid(), PaymentPlanId = plan.Id, DueDate = plan.StartDate, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            db.PaymentOccurrences.Add(new() { Id = Guid.NewGuid(), PaymentPlanId = plan.Id, DueDate = plan.StartDate, CreatedAtUtc = DateTime.UtcNow });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.PaymentPlans.Add(new() { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Invalid FK", Amount = 1, StartDate = plan.StartDate });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            db.PaymentPlans.Add(new() { Id = Guid.NewGuid(), UserId = user.Id, Name = "Invalid amount", Amount = -1, StartDate = plan.StartDate });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.ChangeTracker.Clear();
            await using (var transaction = await db.Database.BeginTransactionAsync())
            {
                db.AuditLogs.Add(new() { Id = Guid.NewGuid(), UserId = user.Id, Action = "Test", EntityType = "Test", CreatedAtUtc = DateTime.UtcNow });
                await db.SaveChangesAsync();
                await transaction.RollbackAsync();
            }
            Assert.Equal(0, await db.AuditLogs.CountAsync());
            var disposablePlan = new PaymentPlan { Id = Guid.NewGuid(), UserId = user.Id, Name = "Delete test", Amount = 1, StartDate = plan.StartDate };
            db.PaymentPlans.Add(disposablePlan);
            await db.SaveChangesAsync();
            db.PaymentPlans.Remove(disposablePlan);
            await db.SaveChangesAsync();
            Assert.False(await db.PaymentPlans.AnyAsync(x => x.Id == disposablePlan.Id));
            await using var factory = CreateFactory(builder.ConnectionString);
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var apiDb = scope.ServiceProvider.GetRequiredService<PayAheadDbContext>();
            Assert.Equal(builder.InitialCatalog, apiDb.Database.GetDbConnection().Database);
            Assert.True(await apiDb.Database.CanConnectAsync());
            Assert.Empty(await apiDb.Database.GetPendingMigrationsAsync());
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        }
        finally
        {
            // Yalnızca bu testin rastgele adla oluşturduğu veritabanı temizlenir.
            if (!db.Database.GetDbConnection().Database.StartsWith("PayAheadTest_", StringComparison.Ordinal))
                throw new InvalidOperationException("Test veritabanı güvenlik kontrolü başarısız.");
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static async Task VerifySchemaAsync(PayAheadDbContext db)
    {
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT name FROM sys.tables WHERE name <> '__EFMigrationsHistory' ORDER BY name";
            await using (var reader = await command.ExecuteReaderAsync())
            {
                var tables = new List<string>();
                while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
                Assert.Equal(new[] { "AuditLogs", "DeviceTokens", "NotificationDeliveries", "PaymentOccurrences", "PaymentPlans", "RefreshTokens", "ReminderRules", "UserSettings", "UserSubscriptions", "Users", "TrackedSubscriptions", "StorePurchases", "StoreTransactions", "StoreWebhookInbox", "RateLimitBuckets" }.OrderBy(x => x, StringComparer.Ordinal), tables.OrderBy(x => x, StringComparer.Ordinal));
            }
            command.CommandText = "SELECT COUNT(*) FROM sys.key_constraints WHERE type = 'PK' AND parent_object_id <> OBJECT_ID('__EFMigrationsHistory')";
            Assert.Equal(15, (int)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sys.foreign_keys WHERE is_disabled = 0 AND is_not_trusted = 0";
            Assert.Equal(14, (int)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('PaymentPlans') AND name = 'Amount' AND precision = 19 AND scale = 4";
            Assert.Equal(1, (int)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sys.columns WHERE object_id IN (OBJECT_ID('PaymentPlans'), OBJECT_ID('PaymentOccurrences')) AND name = 'RowVersion' AND system_type_id = 189 AND max_length = 8 AND is_nullable = 0";
            Assert.Equal(2, (int)(await command.ExecuteScalarAsync())!);
            command.CommandText = "SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('PaymentOccurrences') AND name = 'IX_PaymentOccurrences_PaymentPlanId_DueDate' AND is_unique = 1";
            Assert.Equal(1, (int)(await command.ExecuteScalarAsync())!);
        }
        finally { await db.Database.CloseConnectionAsync(); }
    }

    private static WebApplicationFactory<Program> CreateFactory(string connection) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development").UseRegressionPremium().ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PayAhead"] = connection,
                ["DevelopmentIdentity:Enabled"] = "false"
            })));
}
