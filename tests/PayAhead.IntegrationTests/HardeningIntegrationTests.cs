using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application.Auth;
using PayAhead.Application.Operations;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class HardeningIntegrationTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private string connection = "";
    private readonly string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private readonly Guid user = Guid.NewGuid(), other = Guid.NewGuid();
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private const string Password = "TestPassphrase123!";
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection, x => x.EnableRetryOnFailure(3)).Options);
    private WebApplicationFactory<Program> Host() => new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development")
        .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("Jwt:SigningKey", key).UseSetting("Maintenance:Enabled", "false")
        .UseSetting("Cors:Origins:0", "https://mobile.example.test"));
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
        foreach (var id in new[] { user, other })
        {
            var row = new User { Id = id, Email = id + "@test.invalid", NormalizedEmail = (id + "@test.invalid").ToUpperInvariant(), DisplayName = "Private profile", CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            row.PasswordHash = new IdentityPasswordService().Hash(row, Password); db.Users.Add(row);
            db.UserSettings.Add(new() { UserId = id, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
            db.PaymentPlans.Add(new() { Id = Guid.NewGuid(), UserId = id, Name = id == user ? "own-private-plan" : "other-secret-plan", Amount = 1.2345m, StartDate = DateOnly.FromDateTime(DateTime.UtcNow), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(); factory = Host(); client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
    }
    public async Task DisposeAsync() { client.Dispose(); await factory.DisposeAsync(); await using var db = Db(); Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    [Fact]
    public async Task Export_is_owner_scoped_and_never_serializes_credentials()
    {
        var response = await client.GetAsync("/api/users/me/export"); response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(); Assert.Contains("own-private-plan", body); Assert.DoesNotContain("other-secret-plan", body);
        using var json = JsonDocument.Parse(body); Assert.EndsWith("Z", json.RootElement.GetProperty("profile").GetProperty("createdAtUtc").GetString());
        foreach (var secret in new[] { "passwordHash", "tokenHash", "pushToken", "protectedEvidence", "signingKey" }) Assert.DoesNotContain(secret, body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType); Assert.True(response.Headers.CacheControl?.NoStore);
    }
    [Fact]
    public async Task Export_refuses_excessive_dataset_instead_of_silent_truncation()
    {
        await using (var db = Db()) { for (var i = 0; i < 1000; i++) db.PaymentPlans.Add(new() { Id = Guid.NewGuid(), UserId = user, Name = "Existing", Amount = 1, StartDate = new(2026, 1, 1), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.GetAsync("/api/users/me/export")).StatusCode);
    }
    [Fact]
    public async Task Deletion_requires_password_removes_owned_data_and_invalidates_JWT()
    {
        var wrong = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me") { Content = JsonContent.Create(new DeleteAccountRequest("incorrect-password")) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(wrong)).StatusCode);
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me") { Content = JsonContent.Create(new DeleteAccountRequest(Password)) };
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        await using var db = Db(); var deleted = await db.Users.SingleAsync(x => x.Id == user); Assert.False(deleted.IsActive); Assert.Empty(deleted.PasswordHash); Assert.EndsWith("@deleted.invalid", deleted.Email);
        Assert.False(await db.PaymentPlans.AnyAsync(x => x.UserId == user)); Assert.False(await db.UserSettings.AnyAsync(x => x.UserId == user)); Assert.True(await db.PaymentPlans.AnyAsync(x => x.UserId == other));
        using var scope = factory.Services.CreateScope(); await scope.ServiceProvider.GetRequiredService<IAccountStore>().DeleteAsync(user, "", CancellationToken.None); // service retry is idempotent
    }
    [Fact]
    public async Task Deletion_retains_only_store_anti_replay_binding_and_scrubs_sessions_devices()
    {
        await using (var db = Db())
        {
            var now = DateTime.UtcNow; var root = new UserSubscription { Id = Guid.NewGuid(), UserId = user, Provider = "Google", ExternalSubscriptionId = "original", ProductId = "premium", Status = "Active", ProtectedEvidence = "secret", IsStoreVerified = true };
            db.UserSubscriptions.Add(root); db.StorePurchases.Add(new() { Id = Guid.NewGuid(), UserId = user, UserSubscriptionId = root.Id, Store = "Google", TransactionId = "tx", TokenHash = new('A', 64), CreatedAtUtc = now });
            db.RefreshTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, FamilyId = Guid.NewGuid(), TokenHash = new('B', 64), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(30) });
            db.DeviceTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, Platform = "android", PushToken = "private-device", LastSeenAtUtc = now }); await db.SaveChangesAsync();
        }
        var response = await client.SendAsync(new(HttpMethod.Delete, "/api/users/me") { Content = JsonContent.Create(new DeleteAccountRequest(Password)) }); Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var check = Db(); Assert.False(await check.RefreshTokens.AnyAsync(x => x.UserId == user)); Assert.False(await check.DeviceTokens.AnyAsync(x => x.UserId == user));
        var rootAfter = await check.UserSubscriptions.SingleAsync(); Assert.Null(rootAfter.ProtectedEvidence); Assert.False(rootAfter.IsStoreVerified); Assert.Single(await check.StorePurchases.ToArrayAsync());
    }
    [Fact]
    public async Task Shared_rate_limit_is_atomic_across_independent_SQL_connections()
    {
        var id = new string('A', 64);
        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(async _ => { await using var db = Db(); return await new SqlSharedRateLimiter(db).AcquireAsync(id, 10, CancellationToken.None); }));
        Assert.Equal(10, results.Count(x => x)); await using var check = Db(); Assert.Equal(10, (await check.RateLimitBuckets.SingleAsync()).Count);
        await check.RateLimitBuckets.ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-2)));
        await using var next = Db(); Assert.True(await new SqlSharedRateLimiter(next).AcquireAsync(id, 10, CancellationToken.None));
    }
    [Fact]
    public async Task Two_API_instances_share_auth_budget_and_ignore_spoofed_proxy_IP()
    {
        await using var second = Host(); using var http = second.CreateClient();
        for (var i = 0; i < 20; i++) { var target = i % 2 == 0 ? client : http; target.DefaultRequestHeaders.Remove("X-Forwarded-For"); target.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.0.2.{i}"); Assert.Equal(HttpStatusCode.BadRequest, (await target.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""))).StatusCode); }
        Assert.Equal(HttpStatusCode.TooManyRequests, (await http.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""))).StatusCode);
        await using var db = Db(); Assert.All(await db.RateLimitBuckets.ToArrayAsync(), x => Assert.Equal(64, x.Id.Length));
    }
    [Fact]
    public async Task Cors_headers_and_correlation_are_restricted()
    {
        client.DefaultRequestHeaders.Add("Origin", "https://mobile.example.test"); client.DefaultRequestHeaders.Add("X-Correlation-ID", "safe-correlation-123");
        var good = await client.GetAsync("/api/premium/plans"); Assert.Equal("https://mobile.example.test", good.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(good.Headers.Contains("Access-Control-Allow-Credentials")); Assert.Equal("nosniff", good.Headers.GetValues("X-Content-Type-Options").Single()); Assert.Equal("DENY", good.Headers.GetValues("X-Frame-Options").Single()); Assert.Equal("safe-correlation-123", good.Headers.GetValues("X-Correlation-ID").Single());
        client.DefaultRequestHeaders.Remove("Origin"); client.DefaultRequestHeaders.Add("Origin", "https://attacker.test"); var bad = await client.GetAsync("/api/premium/plans"); Assert.False(bad.Headers.Contains("Access-Control-Allow-Origin"));
    }
    [Fact]
    public async Task Refresh_cleanup_preserves_live_families_and_obeys_batch()
    {
        var now = DateTime.UtcNow; var old = now.AddDays(-200); var family = Guid.NewGuid();
        await using (var db = Db())
        {
            foreach (var i in Enumerable.Range(0, 4)) db.RefreshTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, FamilyId = i < 2 ? family : Guid.NewGuid(), TokenHash = i.ToString().PadLeft(64, '0'), CreatedAtUtc = old, ExpiresAtUtc = old.AddDays(1), RevokedAtUtc = old });
            db.RefreshTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, FamilyId = family, TokenHash = new('F', 64), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(30) }); await db.SaveChangesAsync();
        }
        await using (var db = Db()) Assert.Equal(1, await new MaintenanceStore(db, new() { BatchSize = 1 }).CleanupAsync(now, CancellationToken.None));
        await using (var db = Db()) Assert.Equal(1, await new MaintenanceStore(db, new() { BatchSize = 1 }).CleanupAsync(now, CancellationToken.None));
        await using (var db = Db()) { Assert.Equal(0, await new MaintenanceStore(db, new() { BatchSize = 1 }).CleanupAsync(now, CancellationToken.None)); Assert.Equal(3, await db.RefreshTokens.CountAsync()); }
    }
    private sealed class FailedLimiter : ISharedRateLimiter { public Task<bool> AcquireAsync(string key, int limit, CancellationToken ct) => throw new OperationalUnavailableException(); }
    [Fact]
    public async Task Deletion_direct_store_is_transactionally_valid()
    {
        await using var db = Db(); await new AccountStore(db, new IdentityPasswordService(), TimeProvider.System).DeleteAsync(user, Password, CancellationToken.None);
        Assert.False((await db.Users.AsNoTracking().SingleAsync(x => x.Id == user)).IsActive);
    }
    [Fact]
    public async Task Limiter_outage_fails_closed_on_sensitive_routes_but_liveness_remains_available()
    {
        await using var broken = Host().WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddScoped<ISharedRateLimiter, FailedLimiter>())); using var http = broken.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await http.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);
    }
    [Fact]
    public async Task Oversized_privacy_body_is_rejected_and_SQL_input_remains_data()
    {
        var oversized = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me") { Content = JsonContent.Create(new DeleteAccountRequest(new string('x', 5000))) };
        oversized.Content.Headers.ContentLength = 5022;
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.SendAsync(oversized)).StatusCode);
        var name = "'; DROP TABLE Users; --";
        var result = await client.PostAsJsonAsync("/api/payments", new { name, amount = 1, currencyCode = "TRY", category = "Bills", frequency = "Monthly", interval = 1, startDate = "2026-10-21", isActive = true }); Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        await using var db = Db(); Assert.Equal(2, await db.Users.CountAsync()); Assert.True(await db.PaymentPlans.AnyAsync(x => x.Name == name));
    }
    [Fact]
    public async Task Deletion_failure_rolls_back_profile_and_owned_data()
    {
        await using var db = Db();
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RejectTestDeletion ON PaymentPlans INSTEAD OF DELETE AS THROW 50000, 'Controlled test rollback', 1;");
        await Assert.ThrowsAsync<SqlException>(() => new AccountStore(db, new IdentityPasswordService(), TimeProvider.System).DeleteAsync(user, Password, CancellationToken.None));
        db.ChangeTracker.Clear(); Assert.True((await db.Users.SingleAsync(x => x.Id == user)).IsActive); Assert.True(await db.PaymentPlans.AnyAsync(x => x.UserId == user));
    }
    [Fact]
    public async Task Failed_webhook_inbox_has_lease_recovery_terminal_bound_and_manual_retry()
    {
        // No provider is contacted: after repair this is an authenticated, unbound reference.
        var id = Guid.NewGuid(); var now = DateTime.UtcNow;
        // Test-only encryption key registration, shipping provider remains unchanged.
        var options = new StoreOptions { EncryptionKeys = new() { ["v1"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) } };
        await using var host = Host().WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(new PurchaseProtector(options)))); using var http = host.CreateClient();
        await using (var db = Db()) { db.StoreWebhookInbox.Add(new() { Id = id, Store = "Google", EventId = "poison", ReferenceHash = PurchaseProtector.Hash("unbound"), ProtectedReference = "invalid", CreatedAtUtc = now, NextAttemptAtUtc = now }); await db.SaveChangesAsync(); }
        using (var work = host.Services.CreateScope()) await work.ServiceProvider.GetRequiredService<PayAhead.Application.Premium.IPremiumService>().ProcessAsync(CancellationToken.None);
        await using (var db = Db()) { var row = await db.StoreWebhookInbox.SingleAsync(); Assert.Equal(1, row.AttemptCount); Assert.Equal("ReconcileFailed", row.ErrorCode); row.AttemptCount = 10; row.LeaseOwner = Guid.NewGuid(); row.LeaseExpiresAtUtc = now.AddMinutes(1); row.ProtectedReference = new PurchaseProtector(options).Protect("unbound"); await db.SaveChangesAsync(); }
        using (var work = host.Services.CreateScope()) await work.ServiceProvider.GetRequiredService<PayAhead.Application.Premium.IPremiumService>().ProcessAsync(CancellationToken.None);
        await using (var db = Db())
        {
            Assert.Null((await db.StoreWebhookInbox.SingleAsync()).CompletedAtUtc);
            await db.StoreWebhookInbox.ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseExpiresAtUtc, now.AddMinutes(-1)));
            var script = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../ops/retry-webhook.sql"))).Replace("'$(InboxId)'", "@InboxId", StringComparison.Ordinal);
            await db.Database.ExecuteSqlRawAsync(script, new SqlParameter("InboxId", id));
        }
        using (var work = host.Services.CreateScope()) await work.ServiceProvider.GetRequiredService<PayAhead.Application.Premium.IPremiumService>().ProcessAsync(CancellationToken.None);
        await using (var db = Db()) { var row = await db.StoreWebhookInbox.SingleAsync(); Assert.NotNull(row.CompletedAtUtc); Assert.Equal(1, row.AttemptCount); Assert.Equal("Unbound", row.ErrorCode); }
    }
    [Fact]
    public async Task Phase9_migration_upgrade_preserves_existing_user_and_adds_shared_buckets()
    {
        var upgradedConnection = new SqlConnectionStringBuilder(connection) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(upgradedConnection).Options);
        try
        {
            var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
            await migrator.MigrateAsync("20261021143405_VerifiedPremiumMembership");
            // Insert against the historical schema rather than today's EF model with new columns.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id,Email,NormalizedEmail,DisplayName,PasswordHash,IsActive,FailedLoginCount,LoginLockedUntilUtc,CreatedAtUtc,UpdatedAtUtc) VALUES ({user},{"upgrade@test.invalid"},{"UPGRADE@TEST.INVALID"},{"Preserved"},{""},{true},{0},NULL,{DateTime.UtcNow},{DateTime.UtcNow})");
            await db.Database.MigrateAsync(); Assert.Equal("Preserved", (await db.Users.SingleAsync()).DisplayName); Assert.Empty(await db.RateLimitBuckets.ToArrayAsync());
        }
        finally { Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    }
}
