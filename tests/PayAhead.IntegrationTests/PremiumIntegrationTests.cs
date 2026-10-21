using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application.Premium;
using PayAhead.Application.Auth;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using PayAhead.Testing;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class PremiumIntegrationTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private string connection = ""; private readonly Guid user = Guid.NewGuid(), other = Guid.NewGuid();
    private readonly TestStoreProvider fake = new(RandomNumberGenerator.GetBytes(64));
    private readonly Clock clock = new(); private WebApplicationFactory<Program> factory = null!; private HttpClient client = null!;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private sealed class Clock : TimeProvider { public DateTime Current = DateTime.UtcNow; public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Current, DateTimeKind.Utc)); }
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
        foreach (var id in new[] { user, other }) { db.Users.Add(new() { Id = id, Email = id + "@test.invalid", NormalizedEmail = (id + "@TEST.INVALID").ToUpperInvariant(), DisplayName = "Test", PasswordHash = "seed", CreatedAtUtc = clock.Current, UpdatedAtUtc = clock.Current }); db.UserSettings.Add(new() { UserId = id, TimeZoneId = "UTC", CreatedAtUtc = clock.Current, UpdatedAtUtc = clock.Current }); }
        await db.SaveChangesAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false")
            .UseSetting("Stores:EncryptionKeys:v1", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))).UseSetting("Membership:PremiumPaymentPlans", "6").UseSetting("Membership:PremiumReminders", "3").UseSetting("Membership:PremiumTrackedSubscriptions", "4")
            .ConfigureTestServices(s => { s.AddSingleton<TimeProvider>(clock); s.AddSingleton<IStorePurchaseVerifier>(fake); s.AddSingleton<IStoreWebhookAuthenticator>(fake); }));
        client = factory.CreateClient(); Authenticate(user);
    }
    private void Authenticate(Guid id) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(id).Token);
    public async Task DisposeAsync() { client.Dispose(); await factory.DisposeAsync(); await using var db = Db(); Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    private Task<HttpResponseMessage> Post(string path, object body) => client.PostAsync(path, JsonContent.Create(body, options: Json));
    private Task<HttpResponseMessage> Put(string path, object body) => client.PutAsync(path, JsonContent.Create(body, options: Json));
    private static async Task<T> Read<T>(HttpResponseMessage response) { response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<T>(Json))!; }
    private object Plan(bool active = true) => new { name = "Bill", amount = 1.2345m, currencyCode = "TRY", category = "Bills", frequency = "Monthly", interval = 1, startDate = DateOnly.FromDateTime(clock.Current), isActive = active };
    private async Task<PaymentResponse> CreatePlan(bool active = true) => await Read<PaymentResponse>(await Post("/api/payments", Plan(active)));
    private TestStoreProof Proof(MembershipState state = MembershipState.Active, long revision = 1, string? original = null, Guid? owner = null) => new(owner ?? user, "test.premium", original ?? Guid.NewGuid().ToString("N"), "tx-" + Guid.NewGuid().ToString("N"), clock.Current.AddDays(5), state, revision);
    private Task<HttpResponseMessage> Verify(TestStoreProof proof, string path = "/api/premium/verify-purchase") => Post(path, new PurchaseRequest(proof.Store, proof.ProductId, fake.Sign(proof)));
    private async Task<EntitlementResponse> Me() => await Read<EntitlementResponse>(await client.GetAsync("/api/premium/me"));
    [Fact]
    public async Task Default_is_Free_and_public_catalog_contains_no_secrets()
    {
        var me = await Me(); Assert.False(me.IsPremium); Assert.Equal(new PlanLimits(5, 1, 3), me.Limits); Assert.Equal(user, me.AppAccountToken);
        client.DefaultRequestHeaders.Authorization = null; var catalog = await client.GetAsync("/api/premium/plans"); Assert.Equal(HttpStatusCode.OK, catalog.StatusCode); Assert.DoesNotContain("privateKey", await catalog.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/premium/me")).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/premium/restore", new PurchaseRequest("Google", "test.premium", "proof"))).StatusCode);
    }
    [Fact]
    public async Task Legacy_or_external_subscriptions_never_grant_Premium()
    {
        await using var db = Db(); db.UserSubscriptions.Add(new() { Id = Guid.NewGuid(), UserId = user, Provider = "legacy", ProductId = "premium", ExternalSubscriptionId = "legacy", Status = "Active", CurrentPeriodEndUtc = clock.Current.AddYears(1), LastVerifiedAtUtc = clock.Current }); await db.SaveChangesAsync(); Assert.False((await Me()).IsPremium);
        await Post("/api/subscriptions", Subscription()); Assert.False((await Me()).IsPremium);
    }
    [Fact]
    public async Task Free_payment_limit_is_atomic_under_concurrent_creation()
    {
        for (var i = 0; i < 4; i++) await CreatePlan(); var results = await Task.WhenAll(Post("/api/payments", Plan()), Post("/api/payments", Plan()));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created); var denied = Assert.Single(results, x => x.StatusCode == HttpStatusCode.Forbidden); Assert.Equal("application/problem+json", denied.Content.Headers.ContentType?.MediaType); Assert.Contains("entitlement_limit", await denied.Content.ReadAsStringAsync());
        await using var db = Db(); Assert.Equal(5, await db.PaymentPlans.CountAsync(x => x.IsActive));
    }
    [Fact]
    public async Task Inactive_creation_is_allowed_but_activation_uses_Free_limit()
    {
        for (var i = 0; i < 5; i++) await CreatePlan(); var inactive = await CreatePlan(false);
        Assert.Equal(HttpStatusCode.Forbidden, (await Put($"/api/payments/{inactive.Id}", new { name = inactive.Name, amount = inactive.Amount, currencyCode = "TRY", category = "Bills", frequency = "Monthly", interval = 1, startDate = inactive.StartDate, isActive = true, rowVersion = inactive.RowVersion })).StatusCode);
    }
    [Fact]
    public async Task Existing_over_limit_data_is_retained_and_metadata_edit_is_allowed()
    {
        var first = await CreatePlan(); await using (var db = Db()) { for (var i = 0; i < 5; i++) db.PaymentPlans.Add(new() { Id = Guid.NewGuid(), UserId = user, Name = "Existing", Amount = 1, StartDate = DateOnly.FromDateTime(clock.Current), CreatedAtUtc = clock.Current, UpdatedAtUtc = clock.Current }); await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.OK, (await Put($"/api/payments/{first.Id}", new { name = "Edited", amount = 2, currencyCode = "TRY", category = "Bills", frequency = "Monthly", interval = 1, startDate = first.StartDate, isActive = true, rowVersion = first.RowVersion })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post("/api/payments", Plan())).StatusCode); await using var check = Db(); Assert.Equal(6, await check.PaymentPlans.CountAsync(x => x.IsActive));
    }
    [Fact]
    public async Task Free_reminder_creation_and_activation_share_one_slot()
    {
        var plan = await CreatePlan(); var root = $"/api/payments/{plan.Id}/reminders"; Assert.Equal(HttpStatusCode.Created, (await Post(root, new { daysBefore = 1 })).StatusCode);
        var inactive = await Read<PayAhead.Application.Notifications.ReminderResponse>(await Post(root, new { daysBefore = 3, isEnabled = false }));
        Assert.Equal(HttpStatusCode.Forbidden, (await Put($"{root}/{inactive.Id}", new { daysBefore = 3, isEnabled = true, timeOfDay = "09:00" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(root, new { daysBefore = 7 })).StatusCode);
    }
    private object Subscription() => new { name = "Service", providerName = "Provider", amount = 10, currencyCode = "TRY", billingFrequency = "Monthly", billingInterval = 1, startDate = DateOnly.FromDateTime(clock.Current), trialStartDate = DateOnly.FromDateTime(clock.Current), trialEndDate = DateOnly.FromDateTime(clock.Current.AddDays(7)), firstBillingDate = DateOnly.FromDateTime(clock.Current.AddDays(8)), status = "Trial", autoRenew = true };
    [Fact]
    public async Task Free_tracked_limit_and_trial_reminders_use_central_policy()
    {
        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.Created, (await Post("/api/subscriptions", Subscription())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post("/api/subscriptions", Subscription())).StatusCode);
        await using var db = Db(); Assert.Equal(3, await db.TrackedSubscriptions.CountAsync()); Assert.Equal(3, await db.PaymentPlans.CountAsync()); Assert.Equal(3, await db.ReminderRules.CountAsync(x => x.IsEnabled)); Assert.All(await db.ReminderRules.Where(x => x.IsEnabled).ToArrayAsync(), x => Assert.Equal(1, x.DaysBefore));
    }
    [Fact]
    public async Task Verified_Premium_enforces_configured_technical_limits()
    {
        var grant = await Read<EntitlementResponse>(await Verify(Proof())); Assert.True(grant.IsPremium); Assert.Equal(6, grant.Limits.ActivePaymentPlans);
        for (var i = 0; i < 6; i++) await CreatePlan(); Assert.Equal(HttpStatusCode.Forbidden, (await Post("/api/payments", Plan())).StatusCode);
    }
    [Fact]
    public async Task Premium_reminder_and_tracked_limits_are_enforced()
    {
        await Verify(Proof()); for (var i = 0; i < 4; i++) Assert.Equal(HttpStatusCode.Created, (await Post("/api/subscriptions", Subscription())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post("/api/subscriptions", Subscription())).StatusCode);
        var plan = await CreatePlan(); var path = $"/api/payments/{plan.Id}/reminders";
        foreach (var lead in new[] { 1, 3, 7 }) Assert.Equal(HttpStatusCode.Created, (await Post(path, new { daysBefore = lead })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(path, new { daysBefore = 0 })).StatusCode);
    }
    [Fact]
    public async Task Concurrent_reminders_and_tracked_subscriptions_cannot_exceed_Free_limits()
    {
        var plan = await CreatePlan(); var path = $"/api/payments/{plan.Id}/reminders";
        var reminders = await Task.WhenAll(Post(path, new { daysBefore = 1 }), Post(path, new { daysBefore = 3 }));
        Assert.Single(reminders, x => x.StatusCode == HttpStatusCode.Created); Assert.Single(reminders, x => x.StatusCode == HttpStatusCode.Forbidden);
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.Created, (await Post("/api/subscriptions", Subscription())).StatusCode);
        var tracked = await Task.WhenAll(Post("/api/subscriptions", Subscription()), Post("/api/subscriptions", Subscription()));
        Assert.Single(tracked, x => x.StatusCode == HttpStatusCode.Created); Assert.Single(tracked, x => x.StatusCode == HttpStatusCode.Forbidden);
        await using var db = Db(); Assert.Equal(3, await db.TrackedSubscriptions.CountAsync()); Assert.Equal(4, await db.PaymentPlans.CountAsync());
    }
    [Theory]
    [InlineData(MembershipState.Active, true)]
    [InlineData(MembershipState.GracePeriod, true)]
    [InlineData(MembershipState.BillingRetry, false)]
    [InlineData(MembershipState.Expired, false)]
    [InlineData(MembershipState.Revoked, false)]
    public async Task Store_lifecycle_controls_entitlements(MembershipState state, bool premium)
    { var proof = Proof(state) with { AutoRenew = false }; var me = await Read<EntitlementResponse>(await Verify(proof)); Assert.Equal(premium, me.IsPremium); Assert.False(me.AutoRenew); }
    [Fact]
    public async Task Expiration_is_immediate_and_does_not_delete_user_data()
    {
        await Verify(Proof() with { ExpiresAtUtc = clock.Current.AddSeconds(1) }); await CreatePlan(); clock.Current = clock.Current.AddSeconds(2); Assert.False((await Me()).IsPremium);
        await using var db = Db(); Assert.Single(await db.PaymentPlans.ToArrayAsync()); Assert.Single(await db.UserSubscriptions.ToArrayAsync());
    }
    [Fact]
    public async Task Duplicate_verify_and_restore_are_idempotent_and_encrypted()
    {
        var proof = Proof(); var signed = fake.Sign(proof); await Verify(proof); Assert.True((await Read<EntitlementResponse>(await Verify(proof, "/api/premium/restore"))).IsPremium);
        await using var db = Db(); Assert.Single(await db.UserSubscriptions.ToArrayAsync()); Assert.Single(await db.StorePurchases.ToArrayAsync()); Assert.Single(await db.StoreTransactions.ToArrayAsync());
        var row = await db.UserSubscriptions.SingleAsync(); Assert.DoesNotContain(signed, row.ProtectedEvidence!); Assert.Equal(64, (await db.StorePurchases.SingleAsync()).TokenHash!.Length); Assert.NotEmpty(row.RowVersion);
    }
    [Fact]
    public async Task Cross_user_purchase_binding_is_rejected()
    {
        var proof = Proof(); await Verify(proof); Authenticate(other); Assert.Equal(HttpStatusCode.Conflict, (await Verify(proof)).StatusCode); Assert.False((await Me()).IsPremium);
    }
    [Fact]
    public async Task Webhook_replay_and_out_of_order_events_reconcile_current_store_state()
    {
        var active = Proof(); await Verify(active); var revoked = active with { State = MembershipState.Revoked, Revision = 2, EventId = "refund", TransactionId = "refund-tx" };
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.Accepted, (await Post("/api/webhooks/google/subscriptions", new { signedPayload = fake.Sign(revoked) })).StatusCode);
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IPremiumService>().ProcessAsync(CancellationToken.None);
        Assert.False((await Me()).IsPremium);
        Assert.Equal(HttpStatusCode.Accepted, (await Post("/api/webhooks/google/subscriptions", new { signedPayload = fake.Sign(active with { EventId = "old-active" }) })).StatusCode);
        using (var scope = factory.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<IPremiumService>().ProcessAsync(CancellationToken.None); Assert.False((await Me()).IsPremium);
        await using var db = Db(); Assert.Equal(2, await db.StoreWebhookInbox.CountAsync()); Assert.All(await db.StoreWebhookInbox.ToArrayAsync(), x => Assert.NotNull(x.CompletedAtUtc));
    }
    [Fact]
    public async Task Restore_after_expiration_refreshes_store_state_and_keeps_same_membership()
    {
        var first = Proof() with { ExpiresAtUtc = clock.Current.AddSeconds(1) }; await Verify(first); clock.Current = clock.Current.AddSeconds(2); Assert.False((await Me()).IsPremium);
        var renewed = first with { Revision = 2, ExpiresAtUtc = clock.Current.AddDays(7), TransactionId = "renewed" }; Assert.True((await Read<EntitlementResponse>(await Verify(renewed, "/api/premium/restore"))).IsPremium);
        await using var db = Db(); Assert.Single(await db.UserSubscriptions.ToArrayAsync()); Assert.Equal(2, await db.StoreTransactions.CountAsync());
    }
    [Fact]
    public async Task Client_Premium_fields_and_invalid_proofs_are_rejected()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Post("/api/premium/verify-purchase", new { store = "Google", productId = "test.premium", evidence = "bad", isPremium = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post("/api/premium/verify-purchase", new PurchaseRequest("Fake", "test.premium", "bad"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Verify(Proof() with { ProductId = "wrong" })).StatusCode); Assert.False((await Me()).IsPremium);
    }
    [Fact]
    public async Task Real_shipping_provider_without_credentials_never_grants_fake_access()
    {
        await using var shipping = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false"));
        using var http = shipping.CreateClient(); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", shipping.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await http.PostAsJsonAsync("/api/premium/verify-purchase", new PurchaseRequest("Google", "premium", "token"))).StatusCode);
    }
    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public void Shipping_API_refuses_fake_provider_config_in_every_environment(string environment)
    {
        using var shipping = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment(environment).UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("Stores:UseFake", "true"));
        Assert.Contains("test-assembly", Assert.ThrowsAny<Exception>(() => shipping.CreateClient()).ToString());
    }
    [Fact]
    public async Task Unique_indexes_protect_transaction_and_token_binding()
    {
        await Verify(Proof()); await using var db = Db(); var purchase = await db.StorePurchases.SingleAsync();
        db.StorePurchases.Add(new() { Id = Guid.NewGuid(), UserId = user, UserSubscriptionId = purchase.UserSubscriptionId, Store = "Google", TransactionId = "different", TokenHash = purchase.TokenHash, CreatedAtUtc = clock.Current }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
        db.StoreTransactions.Add(new() { Id = Guid.NewGuid(), UserId = user, UserSubscriptionId = purchase.UserSubscriptionId, Store = "Google", TransactionId = purchase.TransactionId, CreatedAtUtc = clock.Current }); await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task Membership_rowversion_rejects_stale_writer_and_composite_FK_rejects_wrong_owner()
    {
        await Verify(Proof()); await using var first = Db(); await using var second = Db();
        var a = await first.UserSubscriptions.SingleAsync(); var b = await second.UserSubscriptions.SingleAsync();
        a.AutoRenew = false; await first.SaveChangesAsync(); b.Status = "Revoked";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        first.StorePurchases.Add(new() { Id = Guid.NewGuid(), UserId = other, UserSubscriptionId = a.Id, Store = "Google", TransactionId = "wrong-owner", CreatedAtUtc = clock.Current });
        await Assert.ThrowsAsync<DbUpdateException>(() => first.SaveChangesAsync());
    }
    [Fact]
    public async Task Migration_preserves_legacy_membership_as_unverified_Free()
    {
        var legacyConnection = new SqlConnectionStringBuilder(connection) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(legacyConnection).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261021132035_ExternalSubscriptionTracking");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users(Id,Email,NormalizedEmail,DisplayName,PasswordHash,IsActive,CreatedAtUtc,UpdatedAtUtc,FailedLoginCount)VALUES({user},'legacy@test.invalid','LEGACY@TEST.INVALID','Legacy','hash',1,{clock.Current},{clock.Current},0)");
            var id = Guid.NewGuid(); await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO UserSubscriptions(Id,UserId,Provider,ProductId,ExternalSubscriptionId,Status,CurrentPeriodEndUtc,LastVerifiedAtUtc)VALUES({id},{user},'store','premium','legacy','Active',{clock.Current.AddDays(30)},{clock.Current})");
            await db.Database.MigrateAsync(); Assert.Equal(id, (await db.UserSubscriptions.SingleAsync()).Id); Assert.False((await new SqlEntitlementService(db, new(), clock).GetAsync(user, CancellationToken.None)).IsPremium);
        }
        finally { Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    }
}
