using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application.Auth;
using PayAhead.Application.Notifications;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class NotificationIntegrationTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private string connection = "";
    private readonly Guid user = Guid.NewGuid(), other = Guid.NewGuid(), planId = Guid.NewGuid(), ruleId = Guid.NewGuid(), deviceId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 21, 9, 0, 0, DateTimeKind.Utc);
    private const string PushToken = "ExpoPushToken[0123456789abcdefgh]";
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    private static SqlNotificationQueue Queue(PayAheadDbContext db) => new(db, new PaymentRecurrenceCalculator());
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
        db.Users.AddRange(new User { Id = user, Email = "first@example.com", NormalizedEmail = "FIRST@EXAMPLE.COM", DisplayName = "First", PasswordHash = "test-only", CreatedAtUtc = Now, UpdatedAtUtc = Now },
            new User { Id = other, Email = "other@example.com", NormalizedEmail = "OTHER@EXAMPLE.COM", DisplayName = "Other", PasswordHash = "test-only", CreatedAtUtc = Now, UpdatedAtUtc = Now });
        db.UserSettings.Add(new() { UserId = user, TimeZoneId = "UTC", CreatedAtUtc = Now, UpdatedAtUtc = Now });
        db.PaymentPlans.Add(new() { Id = planId, UserId = user, Name = "Private amount", Amount = 123.4567m, StartDate = DateOnly.FromDateTime(Now), CreatedAtUtc = Now, UpdatedAtUtc = Now });
        db.ReminderRules.Add(new() { Id = ruleId, UserId = user, PaymentPlanId = planId, DaysBefore = 0, TimeOfDay = new(9, 0) });
        db.DeviceTokens.Add(new() { Id = deviceId, UserId = user, Platform = "ios", PushToken = PushToken, LastSeenAtUtc = Now }); await db.SaveChangesAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseRegressionPremium().UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false").UseSetting("Notifications:Enabled", "false"));
        client = factory.CreateClient(); Authenticate(user);
    }
    private void Authenticate(Guid id) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(id).Token);
    public async Task DisposeAsync()
    {
        client.Dispose(); await factory.DisposeAsync(); await using var db = Db(); Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync();
    }
    private string Rules => $"/api/payments/{planId}/reminders";
    private async Task Schedule(DateTime? at = null) { await using var db = Db(); await Queue(db).ScheduleBatchAsync(null, at ?? Now, CancellationToken.None); }
    [Fact]
    public async Task Reminder_CRUD_duplicate_and_deleted_rule_are_consistent()
    {
        var response = await client.PostAsJsonAsync(Rules, new ReminderRequest(3)); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rule = (await response.Content.ReadFromJsonAsync<ReminderResponse>())!;
        Assert.Equal("09:00", rule.TimeOfDay);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(Rules, new ReminderRequest(3))).StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<ReminderResponse[]>(Rules))!.Length);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"{Rules}/{rule.Id}", new ReminderRequest(7, "10:30", false))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{Rules}/{rule.Id}")).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<ReminderResponse[]>(Rules))!);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(Rules, new ReminderRequest(7, "10:30"))).StatusCode);
        await using var db = Db(); Assert.True((await db.ReminderRules.FindAsync(rule.Id))!.IsDeleted);
    }
    [Fact]
    public async Task Concurrent_reminder_registration_has_one_winner()
    {
        var results = await Task.WhenAll(client.PostAsJsonAsync(Rules, new ReminderRequest(7)), client.PostAsJsonAsync(Rules, new ReminderRequest(7)));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created); Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
    }
    [Theory]
    [InlineData(-1, "09:00")]
    [InlineData(366, "09:00")]
    [InlineData(1, "24:00")]
    public async Task Invalid_reminder_is_400(int days, string time) => Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Rules, new ReminderRequest(days, time))).StatusCode);
    [Fact]
    public async Task Device_upsert_rotation_delete_and_privacy()
    {
        var result = await client.PostAsJsonAsync("/api/devices", new DeviceRequest("ios", PushToken)); Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.DoesNotContain(PushToken, await result.Content.ReadAsStringAsync());
        var same = (await result.Content.ReadFromJsonAsync<DeviceResponse>())!; Assert.Equal(deviceId, same.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/devices", new DeviceRequest("android", "ExpoPushToken[another0123456789]", deviceId))).StatusCode);
        Assert.Single((await client.GetFromJsonAsync<DeviceResponse[]>("/api/devices"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/devices/{deviceId}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<DeviceResponse[]>("/api/devices"))!);
        await using var db = Db(); var device = await db.DeviceTokens.SingleAsync(); Assert.False(device.IsActive); Assert.StartsWith("deleted:", device.PushToken);
    }
    [Fact]
    public async Task Device_and_reminder_unique_indexes_reject_direct_duplicates()
    {
        await using var db = Db(); db.DeviceTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, Platform = "ios", PushToken = PushToken, LastSeenAtUtc = Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync()); db.ChangeTracker.Clear();
        db.ReminderRules.Add(new() { Id = Guid.NewGuid(), UserId = user, PaymentPlanId = planId, TimeOfDay = new(9, 0) });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task Cross_user_and_anonymous_reminder_device_settings_access_is_blocked()
    {
        Authenticate(other);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Rules)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Rules, new ReminderRequest(1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"{Rules}/{ruleId}", new ReminderRequest(1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{Rules}/{ruleId}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<DeviceResponse[]>("/api/devices"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/devices/{deviceId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/devices", new DeviceRequest("ios", PushToken))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/users/me/settings", new { languageCode = "tr", defaultCurrencyCode = "TRY", timeZoneId = "UTC", notificationsEnabled = true, userId = user })).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        foreach (var path in new[] { Rules, "/api/devices", "/api/users/me/settings" }) Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
    }
    [Fact]
    public async Task Settings_round_trip_fallback_validation_and_cancellation()
    {
        var settings = await client.GetFromJsonAsync<SettingsResponse>("/api/users/me/settings"); Assert.Equal("UTC", settings!.TimeZoneId); Assert.False(settings.UsedTimeZoneFallback);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/users/me/settings", new SettingsRequest("tr", "TRY", "not-a-zone", true))).StatusCode);
        await Schedule();
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/users/me/settings", new SettingsRequest("en", "usd", "Europe/Istanbul", false))).StatusCode);
        await using var db = Db(); Assert.Equal(DeliveryStatus.Cancelled, (await db.NotificationDeliveries.SingleAsync()).Status);
        await db.UserSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.TimeZoneId, "invalid"));
        var fallback = await client.GetFromJsonAsync<SettingsResponse>("/api/users/me/settings"); Assert.True(fallback!.UsedTimeZoneFallback); Assert.Equal("UTC", fallback.TimeZoneId);
        Authenticate(other); var missing = await client.GetFromJsonAsync<SettingsResponse>("/api/users/me/settings"); Assert.True(missing!.UsedTimeZoneFallback);
    }
    [Fact]
    public async Task Scheduling_is_idempotent_and_one_delivery_per_device()
    {
        await using (var db = Db()) { db.DeviceTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, Platform = "android", PushToken = "ExpoPushToken[second0123456789]", LastSeenAtUtc = Now }); await db.SaveChangesAsync(); }
        await Task.WhenAll(Schedule(), Schedule()); await Schedule();
        await using var check = Db(); Assert.Equal(2, await check.NotificationDeliveries.CountAsync());
        var original = await check.NotificationDeliveries.FirstAsync(); check.NotificationDeliveries.Add(new() { Id = Guid.NewGuid(), UserId = user, PaymentPlanId = planId, ReminderRuleId = ruleId, DeviceTokenId = original.DeviceTokenId, DueDate = original.DueDate, ScheduledAtUtc = Now, UpdatedAtUtc = Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => check.SaveChangesAsync());
    }
    [Fact]
    public async Task Claimed_payment_message_targets_only_the_owned_payment_plan()
    {
        await Schedule(); await using var db = Db();
        var queue = Queue(db); var claimed = (await queue.ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None))!;
        var message = await queue.RevalidateAsync(claimed, Now, CancellationToken.None);
        Assert.NotNull(message); Assert.Equal(planId, message.PaymentPlanId); Assert.Null(message.SubscriptionId);
    }
    [Fact]
    public async Task Long_lead_schedules_due_beyond_thirty_days()
    {
        await using (var db = Db()) { await db.PaymentPlans.ExecuteUpdateAsync(s => s.SetProperty(x => x.StartDate, DateOnly.FromDateTime(Now.AddDays(365)))); await db.ReminderRules.ExecuteUpdateAsync(s => s.SetProperty(x => x.DaysBefore, 365)); }
        await Schedule(); await using var check = Db(); var delivery = await check.NotificationDeliveries.SingleAsync(); Assert.Equal(Now, delivery.ScheduledAtUtc); Assert.Equal(DateOnly.FromDateTime(Now.AddDays(365)), delivery.DueDate);
    }
    [Fact]
    public async Task Scan_cursor_covers_multiple_bounded_batches()
    {
        await using (var db = Db())
        {
            for (var i = 0; i < 25; i++) { var id = Guid.NewGuid(); db.PaymentPlans.Add(new() { Id = id, UserId = user, Name = "Batch", Amount = 1, StartDate = DateOnly.FromDateTime(Now), CreatedAtUtc = Now, UpdatedAtUtc = Now }); db.ReminderRules.Add(new() { Id = Guid.NewGuid(), PaymentPlanId = id, UserId = user, TimeOfDay = new(9, 0) }); }
            await db.SaveChangesAsync();
        }
        Guid? cursor = null; var scans = 0;
        do { await using var db = Db(); cursor = await Queue(db).ScheduleBatchAsync(cursor, Now, CancellationToken.None); scans++; } while (cursor is not null && scans < 5);
        Assert.Equal(2, scans); await using var check = Db(); Assert.Equal(26, await check.NotificationDeliveries.CountAsync());
    }
    [Fact]
    public async Task Concurrent_workers_cannot_claim_same_delivery_and_expired_lease_recovers()
    {
        await Schedule(); await using var a = Db(); await using var b = Db();
        var claims = await Task.WhenAll(Queue(a).ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None), Queue(b).ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None));
        var winner = Assert.Single(claims, x => x is not null)!; Assert.Null(await Queue(b).ClaimAsync(Guid.NewGuid(), Now.AddSeconds(119), CancellationToken.None));
        var recovered = await Queue(b).ClaimAsync(Guid.NewGuid(), Now.AddSeconds(121), CancellationToken.None); Assert.NotNull(recovered); Assert.Equal(winner.Id, recovered.Id); Assert.NotEqual(winner.LeaseOwner, recovered.LeaseOwner);
        await Queue(a).CompleteAsync(winner, new(PushResultKind.TicketAccepted, "stale-ticket"), Now.AddSeconds(122), CancellationToken.None);
        await using var check = Db(); Assert.Null((await check.NotificationDeliveries.SingleAsync()).ProviderMessageId);
    }
    [Theory]
    [InlineData("paid")]
    [InlineData("skipped")]
    [InlineData("inactive-plan")]
    [InlineData("disabled-rule")]
    [InlineData("deleted-rule")]
    [InlineData("disabled-settings")]
    [InlineData("inactive-device")]
    [InlineData("inactive-user")]
    [InlineData("changed-date")]
    [InlineData("ended")]
    public async Task State_changes_immediately_before_send_are_suppressed(string change)
    {
        await Schedule(); await using var db = Db(); var queue = Queue(db); var claimed = (await queue.ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None))!;
        await ChangeState(db, change);
        Assert.Null(await queue.RevalidateAsync(claimed, Now, CancellationToken.None));
        Assert.Equal(DeliveryStatus.Cancelled, (await db.NotificationDeliveries.AsNoTracking().SingleAsync()).Status);
    }
    private async Task ChangeState(PayAheadDbContext db, string change)
    {
        switch (change)
        {
            case "paid":
            case "skipped":
                db.PaymentOccurrences.Add(new() { Id = Guid.NewGuid(), PaymentPlanId = planId, DueDate = DateOnly.FromDateTime(Now), Status = change == "paid" ? OccurrenceStatus.Paid : OccurrenceStatus.Skipped, ActualAmount = change == "paid" ? 1m : null, PaidAtUtc = change == "paid" ? Now : null, CreatedAtUtc = Now }); await db.SaveChangesAsync(); break;
            case "inactive-plan": await db.PaymentPlans.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false)); break;
            case "disabled-rule": await db.ReminderRules.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsEnabled, false)); break;
            case "deleted-rule": await db.ReminderRules.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDeleted, true)); break;
            case "disabled-settings": await db.UserSettings.ExecuteUpdateAsync(s => s.SetProperty(x => x.NotificationsEnabled, false)); break;
            case "inactive-device": await db.DeviceTokens.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false)); break;
            case "inactive-user": await db.Users.Where(x => x.Id == user).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false)); break;
            case "changed-date": await db.PaymentPlans.ExecuteUpdateAsync(s => s.SetProperty(x => x.StartDate, DateOnly.FromDateTime(Now.AddDays(1)))); break;
            case "ended": await db.PaymentPlans.ExecuteUpdateAsync(s => s.SetProperty(x => x.StartDate, DateOnly.FromDateTime(Now.AddDays(-2))).SetProperty(x => x.EndDate, DateOnly.FromDateTime(Now.AddDays(-1)))); break;
        }
    }
    [Fact]
    public async Task Timezone_and_rule_updates_reschedule_and_amount_never_leaks_in_payload()
    {
        await Schedule(); await client.PutAsJsonAsync("/api/users/me/settings", new SettingsRequest("tr", "TRY", "Europe/Istanbul", true)); await Schedule();
        await using var db = Db(); Assert.Equal(Now.AddHours(-3), (await db.NotificationDeliveries.SingleAsync()).ScheduledAtUtc);
        await db.PaymentPlans.ExecuteUpdateAsync(s => s.SetProperty(x => x.Amount, 987.6543m));
        await client.PutAsJsonAsync($"{Rules}/{ruleId}", new ReminderRequest(0, "15:00")); await Schedule();
        var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(Now.AddHours(3), row.ScheduledAtUtc); Assert.Null(await Queue(db).ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None));
    }
    [Fact]
    public async Task Rule_and_device_deletion_cancel_existing_queue_and_stop_new_scheduling()
    {
        await Schedule(); await client.DeleteAsync($"{Rules}/{ruleId}"); await Schedule();
        await using var db = Db(); Assert.Equal(DeliveryStatus.Cancelled, (await db.NotificationDeliveries.SingleAsync()).Status);
        await client.DeleteAsync($"/api/devices/{deviceId}"); Assert.False((await db.DeviceTokens.AsNoTracking().SingleAsync()).IsActive);
    }
    private sealed class Clock : TimeProvider { public DateTime Current = Now; public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Current, DateTimeKind.Utc)); }
    private sealed class FakePushNotificationSender : IPushNotificationSender
    {
        public int Sends, Receipts; public PushResult Result = new(PushResultKind.TicketAccepted, "test-ticket");
        public Task<PushResult> SendAsync(PushMessage message, CancellationToken ct) { Sends++; return Task.FromResult(Result); }
        public Task<PushResult> GetReceiptAsync(string id, CancellationToken ct) { Receipts++; return Task.FromResult(new PushResult(PushResultKind.ReceiptAccepted)); }
    }
    [Fact]
    public async Task Hosted_worker_schedules_and_processes_real_SQL_with_test_only_fake_sender()
    {
        var fake = new FakePushNotificationSender();
        await using var workerFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseRegressionPremium()
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false")
            .UseSetting("Notifications:Enabled", "true").UseSetting("Notifications:AccessToken", "test-only-not-a-real-credential")
            .UseSetting("Notifications:PollSeconds", "5").ConfigureTestServices(services =>
            { services.AddSingleton<IPushNotificationSender>(fake); services.AddSingleton<TimeProvider>(new Clock()); }));
        using var http = workerFactory.CreateClient(); Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);
        var accepted = false;
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await using var db = Db(); accepted = await db.NotificationDeliveries.AnyAsync(x => x.Status == DeliveryStatus.AwaitingReceipt);
            if (accepted) break; await Task.Delay(100);
        }
        Assert.True(accepted); Assert.Equal(1, fake.Sends);
    }
    [Fact]
    public async Task Processor_fake_send_ticket_then_receipt_are_distinct_persistent_states()
    {
        await Schedule(); await using var db = Db(); var fake = new FakePushNotificationSender(); var clock = new Clock(); var processor = new NotificationProcessor(Queue(db), fake, clock);
        Assert.True(await processor.ProcessOneAsync(CancellationToken.None)); var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(DeliveryStatus.AwaitingReceipt, row.Status); Assert.Null(row.SentAtUtc);
        Assert.False(await processor.ProcessOneAsync(CancellationToken.None)); clock.Current = Now.AddMinutes(15); Assert.True(await processor.ProcessOneAsync(CancellationToken.None));
        row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(DeliveryStatus.Sent, row.Status); Assert.NotNull(row.SentAtUtc); Assert.Equal(1, fake.Sends); Assert.Equal(1, fake.Receipts);
        await Schedule(Now.AddMinutes(16)); Assert.False(await processor.ProcessOneAsync(CancellationToken.None));
    }
    [Theory]
    [InlineData(PushResultKind.Retryable, DeliveryStatus.Pending)]
    [InlineData(PushResultKind.Permanent, DeliveryStatus.Failed)]
    [InlineData(PushResultKind.InvalidDevice, DeliveryStatus.Failed)]
    public async Task Retry_and_terminal_errors_are_persisted_without_provider_free_text(PushResultKind kind, DeliveryStatus expected)
    {
        await Schedule(); await using var db = Db(); var fake = new FakePushNotificationSender { Result = new(kind, ErrorCode: kind == PushResultKind.InvalidDevice ? "DeviceNotRegistered" : "private token could be here") };
        await new NotificationProcessor(Queue(db), fake, new Clock()).ProcessOneAsync(CancellationToken.None);
        var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(expected, row.Status); Assert.DoesNotContain("private", row.LastErrorCode!);
        if (kind == PushResultKind.Retryable) { Assert.Equal(Now.AddSeconds(30), row.NextAttemptAtUtc); Assert.Null(await Queue(db).ClaimAsync(Guid.NewGuid(), Now.AddSeconds(29), CancellationToken.None)); }
        if (kind == PushResultKind.InvalidDevice) Assert.False((await db.DeviceTokens.AsNoTracking().SingleAsync()).IsActive);
    }
    [Fact]
    public async Task Retry_stops_after_five_attempts_and_schedule_scan_does_not_reset_backoff()
    {
        await Schedule(); var clock = new Clock(); var fake = new FakePushNotificationSender { Result = new(PushResultKind.Retryable, ErrorCode: "ProviderUnavailable") };
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await using var db = Db(); await new NotificationProcessor(Queue(db), fake, clock).ProcessOneAsync(CancellationToken.None);
            var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(attempt, row.AttemptCount);
            await Schedule(clock.Current);
            if (attempt < 5) clock.Current = row.NextAttemptAtUtc!.Value;
            else Assert.Equal(DeliveryStatus.Failed, row.Status);
        }
        Assert.Equal(5, fake.Sends);
    }
    [Theory]
    [InlineData(PushResultKind.PendingReceipt, false)]
    [InlineData(PushResultKind.Retryable, false)]
    [InlineData(PushResultKind.Resend, true)]
    public async Task Receipt_polling_never_resends_unless_provider_reports_retryable_handoff_failure(PushResultKind result, bool resend)
    {
        await Schedule(); await using var db = Db(); var queue = Queue(db);
        var first = (await queue.ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None))!;
        await queue.CompleteAsync(first, new(PushResultKind.TicketAccepted, "test-ticket"), Now, CancellationToken.None);
        var receipt = (await queue.ClaimAsync(Guid.NewGuid(), Now.AddMinutes(15), CancellationToken.None))!;
        await queue.CompleteAsync(receipt, new(result, ErrorCode: "MessageRateExceeded"), Now.AddMinutes(15), CancellationToken.None);
        var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(resend ? DeliveryStatus.Pending : DeliveryStatus.AwaitingReceipt, row.Status);
        Assert.Equal(resend ? null : "test-ticket", row.ProviderMessageId); Assert.Equal(1, row.AttemptCount); Assert.Equal(1, row.ReceiptAttemptCount);
    }
    [Fact]
    public async Task Receipt_missing_after_twenty_four_hours_is_failed_without_resending()
    {
        await Schedule(); await using var db = Db(); var queue = Queue(db); var first = (await queue.ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None))!;
        await queue.CompleteAsync(first, new(PushResultKind.TicketAccepted, "test-ticket"), Now, CancellationToken.None);
        var receipt = (await queue.ClaimAsync(Guid.NewGuid(), Now.AddHours(24), CancellationToken.None))!;
        await queue.CompleteAsync(receipt, new(PushResultKind.PendingReceipt), Now.AddHours(24), CancellationToken.None);
        var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(DeliveryStatus.Failed, row.Status); Assert.Equal("ReceiptExpired", row.LastErrorCode); Assert.Equal(1, row.AttemptCount);
    }
    [Fact]
    public async Task Configuration_cancellation_fences_old_lease_completion()
    {
        await Schedule(); await using var db = Db(); var queue = Queue(db); var claimed = (await queue.ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None))!;
        await client.PutAsJsonAsync("/api/users/me/settings", new SettingsRequest("tr", "TRY", "UTC", false));
        await queue.CompleteAsync(claimed, new(PushResultKind.TicketAccepted, "late-provider-ticket"), Now, CancellationToken.None);
        var row = await db.NotificationDeliveries.AsNoTracking().SingleAsync(); Assert.Equal(DeliveryStatus.Cancelled, row.Status); Assert.Null(row.ProviderMessageId);
    }
    [Fact]
    public async Task Delivery_device_foreign_key_also_enforces_user_ownership()
    {
        await using var db = Db(); var otherDevice = Guid.NewGuid(); db.DeviceTokens.Add(new() { Id = otherDevice, UserId = other, Platform = "ios", PushToken = "ExpoPushToken[foreign0123456789]", LastSeenAtUtc = Now }); await db.SaveChangesAsync();
        db.NotificationDeliveries.Add(new() { Id = Guid.NewGuid(), UserId = user, PaymentPlanId = planId, ReminderRuleId = ruleId, DeviceTokenId = otherDevice, DueDate = DateOnly.FromDateTime(Now), ScheduledAtUtc = Now, UpdatedAtUtc = Now });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    [Fact]
    public async Task Cleanup_retains_dedup_tombstones_through_schedule_horizon()
    {
        await Schedule(); await using var db = Db(); await db.NotificationDeliveries.ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, DeliveryStatus.Sent).SetProperty(x => x.UpdatedAtUtc, Now.AddDays(-500)));
        Assert.Equal(0, await Queue(db).CleanupAsync(Now, CancellationToken.None));
        await db.NotificationDeliveries.ExecuteUpdateAsync(s => s.SetProperty(x => x.DueDate, DateOnly.FromDateTime(Now.AddDays(-500))));
        Assert.Equal(1, await Queue(db).CleanupAsync(Now, CancellationToken.None));
    }
    [Fact]
    public async Task Migration_upgrades_legacy_delivery_without_deleting_or_sending_it()
    {
        var legacyConnection = new SqlConnectionStringBuilder(connection) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(legacyConnection).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261021121138_AuthenticationSessions");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id,Email,NormalizedEmail,DisplayName,PasswordHash,IsActive,CreatedAtUtc,UpdatedAtUtc,FailedLoginCount) VALUES ({user},'legacy@ex.com','LEGACY@EX.COM','Legacy','hash',1,{Now},{Now},0)");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentPlans (Id,UserId,Name,Amount,CurrencyCode,Category,Frequency,Interval,StartDate,IsActive,CreatedAtUtc,UpdatedAtUtc) VALUES ({planId},{user},'Legacy',1,'TRY','Other',0,1,{DateOnly.FromDateTime(Now)},1,{Now},{Now})");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ReminderRules (Id,UserId,PaymentPlanId,DaysBefore,TimeOfDay,IsEnabled) VALUES ({ruleId},{user},{planId},0,{new TimeOnly(9, 0)},1)");
            var deliveryId = Guid.NewGuid(); await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO NotificationDeliveries (Id,UserId,PaymentPlanId,ReminderRuleId,DueDate,ScheduledAtUtc,Status,AttemptCount) VALUES ({deliveryId},{user},{planId},{ruleId},{DateOnly.FromDateTime(Now)},{Now},0,0)");
            await db.Database.MigrateAsync(); var row = await db.NotificationDeliveries.SingleAsync(); Assert.Equal(deliveryId, row.Id); Assert.Null(row.DeviceTokenId); Assert.Equal(DeliveryStatus.Cancelled, row.Status); Assert.Null(await Queue(db).ClaimAsync(Guid.NewGuid(), Now, CancellationToken.None));
            Assert.Single(await db.Users.ToArrayAsync()); Assert.Single(await db.ReminderRules.ToArrayAsync()); Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally { Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync(); }
    }
}
