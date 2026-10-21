using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Api;
using PayAhead.Application.Auth;
using PayAhead.Application.Notifications;
using PayAhead.Application.Premium;
using PayAhead.Domain;
using PayAhead.Infrastructure;

// Local-only load runner. It creates and removes only its own random database, never calls stores/Expo.
var server = Environment.GetEnvironmentVariable("PAYAHEAD_LOAD_SQLSERVER") ?? throw new InvalidOperationException("Set local test SQL connection.");
var connection = new SqlConnectionStringBuilder(server) { InitialCatalog = "PayAheadLoad_" + Guid.NewGuid().ToString("N") }.ConnectionString;
var database = new SqlConnectionStringBuilder(connection).InitialCatalog;
var user = Guid.NewGuid(); var now = DateTime.UtcNow; var today = DateOnly.FromDateTime(now);
await using var db = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
WebApplication? app = null;
try
{
    await db.Database.MigrateAsync();
    db.Users.Add(new() { Id = user, Email = "load@test.invalid", NormalizedEmail = "LOAD@TEST.INVALID", DisplayName = "Load", CreatedAtUtc = now, UpdatedAtUtc = now });
    db.UserSettings.Add(new() { UserId = user, TimeZoneId = "UTC", CreatedAtUtc = now, UpdatedAtUtc = now });
    for (var i = 0; i < 1000; i++)
    {
        var id = Guid.NewGuid(); db.PaymentPlans.Add(new() { Id = id, UserId = user, Name = "Load plan", Amount = 1.2345m, Frequency = PaymentFrequency.Daily, Interval = 1, StartDate = today.AddDays(-10), CreatedAtUtc = now, UpdatedAtUtc = now });
        db.ReminderRules.Add(new() { Id = Guid.NewGuid(), UserId = user, PaymentPlanId = id, DaysBefore = 0, TimeOfDay = new(0, 0) });
        for (var day = 1; day <= 10; day++) db.PaymentOccurrences.Add(new() { Id = Guid.NewGuid(), PaymentPlanId = id, DueDate = today.AddDays(-day), Status = OccurrenceStatus.Paid, ActualAmount = 1.2345m, PaidAtUtc = now, CreatedAtUtc = now });
    }
    db.DeviceTokens.Add(new() { Id = Guid.NewGuid(), UserId = user, Platform = "android", PushToken = "local-controlled-test", LastSeenAtUtc = now });
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    var encryption = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    app = PayAheadApi.Build([], b =>
    {
        b.Environment.EnvironmentName = "Development";
        b.Configuration["Serilog:MinimumLevel:Default"] = "Warning";
        b.Configuration["Serilog:MinimumLevel:Override:Microsoft"] = "Warning";
        b.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:PayAhead"] = connection, ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), ["Notifications:Enabled"] = "false", ["Stores:WorkerEnabled"] = "false", ["Stores:EncryptionKeys:v1"] = encryption, ["Maintenance:Enabled"] = "false" });
        b.Logging.ClearProviders(); b.WebHost.UseUrls("http://127.0.0.1:0");
    }, s => s.AddSingleton<IPushNotificationSender, ControlledPush>());
    await app.StartAsync();
    using var http = new HttpClient { BaseAddress = new(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(60) };
    http.DefaultRequestHeaders.Authorization = new("Bearer", app.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Token);
    (await http.GetAsync("/health/ready")).EnsureSuccessStatusCode();
    (await http.GetAsync("/api/payments?pageSize=20")).EnsureSuccessStatusCode(); // warmup
    var report = new List<object>();
    foreach (var concurrency in new[] { 100, 500 })
    {
        var process = Process.GetCurrentProcess(); var cpu = process.TotalProcessorTime; var watch = Stopwatch.StartNew();
        var results = await Task.WhenAll(Enumerable.Range(0, concurrency).Select(async _ =>
        {
            var timer = Stopwatch.StartNew();
            try { using var result = await http.GetAsync("/api/payments?pageSize=20"); await result.Content.LoadIntoBufferAsync(); return (ok: result.IsSuccessStatusCode, ms: timer.Elapsed.TotalMilliseconds); }
            catch (HttpRequestException) { return (ok: false, ms: timer.Elapsed.TotalMilliseconds); }
            catch (TaskCanceledException) { return (ok: false, ms: timer.Elapsed.TotalMilliseconds); }
        }));
        watch.Stop(); process.Refresh(); var sorted = results.Select(x => x.ms).Order().ToArray();
        report.Add(new { concurrency, requests = results.Length, errors = results.Count(x => !x.ok), errorRate = results.Count(x => !x.ok) / (double)results.Length, elapsedMs = watch.Elapsed.TotalMilliseconds, p50Ms = sorted[(int)(sorted.Length * .50)], p95Ms = sorted[(int)(sorted.Length * .95)], p99Ms = sorted[Math.Min(sorted.Length - 1, (int)(sorted.Length * .99))], cpuCorePercent = (process.TotalProcessorTime - cpu).TotalMilliseconds / watch.Elapsed.TotalMilliseconds * 100, workingSetBytes = process.WorkingSet64, peakWorkingSetBytes = process.PeakWorkingSet64 });
    }
    using var scope = app.Services.CreateScope(); var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
    var batch = Stopwatch.StartNew(); await queue.ScheduleBatchAsync(null, now, CancellationToken.None);
    var processor = scope.ServiceProvider.GetRequiredService<NotificationProcessor>(); var processed = 0;
    while (processed < 50 && await processor.ProcessOneAsync(CancellationToken.None)) processed++;
    batch.Stop(); report.Add(new { worker = "notification", scheduleRulesLimit = 20, processingLimit = 50, processed, elapsedMs = batch.Elapsed.TotalMilliseconds, provider = "controlled test double; no Expo call" });
    var protector = scope.ServiceProvider.GetRequiredService<PurchaseProtector>();
    for (var i = 0; i < 50; i++) db.StoreWebhookInbox.Add(new() { Id = Guid.NewGuid(), Store = "Google", EventId = "local-" + i, ReferenceHash = PurchaseProtector.Hash("unbound-" + i), ProtectedReference = protector.Protect("unbound-" + i), CreatedAtUtc = now, NextAttemptAtUtc = now });
    await db.SaveChangesAsync(); var inboxWatch = Stopwatch.StartNew();
    for (var i = 0; i < 50; i++) await scope.ServiceProvider.GetRequiredService<IPremiumService>().ProcessAsync(CancellationToken.None);
    inboxWatch.Stop(); report.Add(new { worker = "webhook", processed = await db.StoreWebhookInbox.CountAsync(x => x.CompletedAtUtc != null), elapsedMs = inboxWatch.Elapsed.TotalMilliseconds, provider = "unbound events; no store call" });
    Directory.CreateDirectory("artifacts");
    await File.WriteAllTextAsync("artifacts/phase10-load.json", JsonSerializer.Serialize(new { timestampUtc = DateTime.UtcNow, dataset = new { plans = 1000, occurrences = 10000, rules = 1000 }, processScope = "API and load client in same process; SQL server separate; CPU is percent of one logical core", results = report }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Local load evidence: artifacts/phase10-load.json");
}
finally
{
    if (app is not null) { await app.StopAsync(); await app.DisposeAsync(); }
    if (!database.StartsWith("PayAheadLoad_", StringComparison.Ordinal) || database.Length != 45) throw new InvalidOperationException("Unsafe cleanup target.");
    await db.Database.EnsureDeletedAsync();
}
sealed class ControlledPush : IPushNotificationSender
{
    public Task<PushResult> SendAsync(PushMessage message, CancellationToken ct) => Task.FromResult(new PushResult(PushResultKind.TicketAccepted, "local-" + message.DeliveryId));
    public Task<PushResult> GetReceiptAsync(string id, CancellationToken ct) => Task.FromResult(new PushResult(PushResultKind.ReceiptAccepted));
}
