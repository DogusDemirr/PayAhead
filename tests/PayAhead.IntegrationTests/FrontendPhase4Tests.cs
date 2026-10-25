using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PayAhead.Application.Auth;
using PayAhead.Application.Overview;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class FrontendPhase4Tests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private string connection = "";
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private readonly CaptureEmail email = new();
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(sql.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseRegressionPremium()
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)))
            .UseSetting("DevelopmentIdentity:Enabled", "false").UseSetting("PasswordReset:LinkBase", "exp://127.0.0.1:8085/--/reset-password")
            .ConfigureTestServices(s => s.AddSingleton<IPasswordResetEmail>(email)));
        client = factory.CreateClient();
    }
    public async Task DisposeAsync()
    {
        client.Dispose(); await factory.DisposeAsync(); await using var db = Db();
        Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync();
    }
    private async Task<AuthResponse> Register(string address = "phase4@example.com")
    {
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(address, "old-password-long!", "Phase 4"))).StatusCode);
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(address, "old-password-long!"));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
    private async Task Forgot(string address = "phase4@example.com")
    {
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(address))).StatusCode);
        if (address == "phase4@example.com") await WaitForEmail();
    }
    private async Task WaitForEmail()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (email.Links.IsEmpty) await Task.Delay(10, deadline.Token);
    }
    private string Token => email.Links.Single().Split("token=")[1];
    [Fact]
    public async Task Missing_reset_migration_returns_same_safe_503_for_known_and_unknown_addresses()
    {
        await Register();
        await using var db = Db();
        await db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync("20261021152026_ProductionOperations");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        foreach (var address in new[] { "phase4@example.com", "absent@example.com" })
        {
            var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(address));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("Şifre yenileme hizmeti şu anda kullanılamıyor.", problem.GetProperty("title").GetString());
            Assert.DoesNotContain("PasswordResetTokens", await response.Content.ReadAsStringAsync());
        }
        Assert.Empty(email.Links); Assert.Equal(1, await db.Users.CountAsync());
    }
    [Fact]
    public async Task Real_Kestrel_http_smoke_uses_isolated_SQL_and_new_endpoints()
    {
        await using var app = PayAhead.Api.PayAheadApi.Build([], builder =>
        {
            builder.Environment.EnvironmentName = "Development";
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PayAhead"] = connection, ["DevelopmentIdentity:Enabled"] = "false", ["AllowedHosts"] = "127.0.0.1;localhost",
                ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), ["PasswordReset:LinkBase"] = "exp://127.0.0.1:8085/--/reset-password",
                ["Notifications:Enabled"] = "false", ["Stores:WorkerEnabled"] = "false", ["Serilog:MinimumLevel:Default"] = "Warning"
            });
        }, services => services.AddSingleton<IPasswordResetEmail>(email));
        await app.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
            Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/ready")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/swagger/v1/swagger.json")).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await http.PostAsJsonAsync("/api/auth/register", new RegisterRequest("tcp@example.test", "old-password-long!", "TCP smoke"))).StatusCode);
            var login = (await (await http.PostAsJsonAsync("/api/auth/login", new LoginRequest("tcp@example.test", "old-password-long!"))).Content.ReadFromJsonAsync<AuthResponse>())!;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/calendar?year=2026&month=10")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/analytics?year=2026&month=10")).StatusCode);
            http.DefaultRequestHeaders.Authorization = null;
            Assert.Equal(HttpStatusCode.Accepted, (await http.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest("tcp@example.test"))).StatusCode);
            await WaitForEmail();
            Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(Token, "new-password-long!"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await http.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsJsonAsync("/api/auth/login", new LoginRequest("tcp@example.test", "new-password-long!"))).StatusCode);
        }
        finally { await app.StopAsync(); }
    }
    [Fact]
    public async Task Additive_reset_migration_preserves_existing_users_and_plans()
    {
        await using var db = Db();
        var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await migrator.MigrateAsync("20261021152026_ProductionOperations");
        var id = Guid.NewGuid(); var now = DateTime.UtcNow;
        db.Users.Add(new() { Id = id, Email = "migration@example.test", NormalizedEmail = "MIGRATION@EXAMPLE.TEST", DisplayName = "Retained", PasswordHash = "retained-hash", CreatedAtUtc = now, UpdatedAtUtc = now });
        await db.SaveChangesAsync();
        // Insert using the historical schema; the current EF model has later nullable catalog columns.
        var retainedAmount = new SqlParameter("RetainedAmount", System.Data.SqlDbType.Decimal) { Precision = 19, Scale = 4, Value = 10.1234m };
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO PaymentPlans (Id,UserId,Name,Amount,CurrencyCode,Category,Frequency,Interval,StartDate,IsActive,CreatedAtUtc,UpdatedAtUtc) VALUES ({Guid.NewGuid()},{id},{"Retained"},{retainedAmount},{"TRY"},{"Bills"},{0},{1},{DateOnly.FromDateTime(now)},{true},{now},{now})");
        await db.Database.MigrateAsync();
        Assert.Equal("retained-hash", (await db.Users.SingleAsync()).PasswordHash); Assert.Equal(10.1234m, (await db.PaymentPlans.SingleAsync()).Amount);
        Assert.Empty(await db.Set<PasswordResetToken>().ToArrayAsync()); Assert.False(db.Database.HasPendingModelChanges());
    }
    [Fact]
    public async Task Forgot_response_is_generic_and_only_hash_is_persisted_with_cooldown()
    {
        await Register();
        var missing = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest("absent@example.com"));
        var known = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest("phase4@example.com"));
        Assert.Equal(missing.StatusCode, known.StatusCode); Assert.Equal(await missing.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());
        await WaitForEmail(); Assert.Equal(43, Token.Length); Assert.Matches("^[A-Za-z0-9_-]{43}$", Token);
        await Forgot(); Assert.Single(email.Links);
        await using var db = Db(); var row = await db.Set<PasswordResetToken>().SingleAsync();
        Assert.Equal(PasswordResetService.Hash(Token), row.TokenHash); Assert.NotEqual(Token, row.TokenHash);
        Assert.Equal(TimeSpan.FromMinutes(15), row.ExpiresAtUtc - row.CreatedAtUtc);
        Assert.Null(row.ConsumedAtUtc); Assert.False(db.Database.HasPendingModelChanges());
    }
    [Fact]
    public async Task Reset_is_one_use_changes_password_and_revokes_every_refresh_family()
    {
        var first = await Register(); var second = (await (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("phase4@example.com", "old-password-long!"))).Content.ReadFromJsonAsync<AuthResponse>())!;
        await Forgot(); var raw = Token;
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(raw, "new-password-long!"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(raw, "other-password-long!"))).StatusCode);
        foreach (var session in new[] { first, second }) Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(session.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("phase4@example.com", "old-password-long!"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("phase4@example.com", "new-password-long!"))).StatusCode);
    }
    [Fact]
    public async Task Concurrent_reset_has_one_winner()
    {
        await Register(); await Forgot(); var body = new ResetPasswordRequest(Token, "new-password-long!");
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/auth/reset-password", body), client.PostAsJsonAsync("/api/auth/reset-password", body));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NoContent); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task Expired_unknown_and_consumed_tokens_share_safe_error_and_weak_password_does_not_consume()
    {
        await Register(); await Forgot(); var token = Token;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, "short"))).StatusCode);
        await using var db = Db(); Assert.Null((await db.Set<PasswordResetToken>().SingleAsync()).ConsumedAtUtc);
        await db.Set<PasswordResetToken>().ExecuteUpdateAsync(s => s.SetProperty(x => x.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        var expired = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(token, "new-password-long!"));
        var unknown = await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(new string('a', 43), "new-password-long!"));
        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        var a = await expired.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(); var b = await unknown.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(a.GetProperty("title").GetString(), b.GetProperty("title").GetString()); Assert.DoesNotContain(token, await expired.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task Delivery_failure_remains_generic_and_makes_token_unusable()
    {
        await Register(); email.Success = false; await Forgot();
        await using var db = Db();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (await db.Set<PasswordResetToken>().AsNoTracking().AnyAsync(x => x.ConsumedAtUtc == null, deadline.Token)) await Task.Delay(10, deadline.Token);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(Token, "new-password-long!"))).StatusCode);
    }
    [Fact]
    public async Task Forgot_and_reset_use_shared_auth_rate_limit()
    {
        for (var i = 0; i < 20; i++) await Forgot("unknown@example.com");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest(new string('a', 43), "new-password-long!"))).StatusCode);
    }
    [Fact]
    public async Task Analytics_requires_auth_validates_and_never_returns_another_users_money()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/analytics?year=2026&month=10")).StatusCode);
        var owner = await Register(); var other = await Register("other@example.com");
        await using var db = Db(); var now = DateTime.UtcNow; var due = new DateOnly(2026, 10, 1);
        PaymentPlan Plan(Guid user, string currency, decimal amount) => new() { Id = Guid.NewGuid(), UserId = user, Name = "Plan", Amount = amount, CurrencyCode = currency, Category = "Bills", Frequency = PaymentFrequency.Once, StartDate = due, CreatedAtUtc = now, UpdatedAtUtc = now };
        var paid = Plan(owner.User.Id, "TRY", 10.1234m); var pending = Plan(owner.User.Id, "TRY", 20); var skipped = Plan(owner.User.Id, "TRY", 30); var usd = Plan(owner.User.Id, "USD", 5); var foreign = Plan(other.User.Id, "TRY", 99999);
        db.AddRange(paid, pending, skipped, usd, foreign); await db.SaveChangesAsync();
        db.AddRange(PaymentOccurrence.Create(paid.Id, due, OccurrenceStatus.Paid, 7.4321m, now), PaymentOccurrence.Create(skipped.Id, due, OccurrenceStatus.Skipped, null, now)); await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/analytics?year=2026&month=13")).StatusCode);
        var result = await client.GetFromJsonAsync<AnalyticsResponse>("/api/analytics?year=2026&month=10");
        var tr = result!.Currencies.Single(x => x.CurrencyCode == "TRY"); var month = tr.Months.Last();
        Assert.Equal(60.1234m, month.PlannedAmount); Assert.Equal(7.4321m, month.PaidAmount); Assert.Equal(20, month.PendingAmount);
        Assert.Equal(1, month.PaidCount); Assert.Equal(1, month.SkippedCount); Assert.Equal(1, month.PendingCount);
        Assert.Equal(7.4321m, Assert.Single(tr.PaidCategories).Amount); Assert.Equal(5, result.Currencies.Single(x => x.CurrencyCode == "USD").Months.Last().PendingAmount);
        Assert.Equal(6, tr.Months.Count); Assert.All(tr.Months.Take(5), x => Assert.Equal(0, x.PlannedAmount));
        Assert.Equal(2, await db.PaymentOccurrences.CountAsync());
    }
    [Fact]
    public async Task Analytics_has_a_user_rate_limit()
    {
        var owner = await Register(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
        for (var i = 0; i < 30; i++) Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/analytics?year=2026&month=10")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/analytics?year=2026&month=10")).StatusCode);
    }
    [Fact]
    public async Task Real_smtp_transport_is_captured_locally_without_claiming_inbox_delivery()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var capture = Task.Run(async () =>
        {
            using var socket = await listener.AcceptTcpClientAsync(deadline.Token); using var stream = socket.GetStream();
            using var reader = new StreamReader(stream); using var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\r\n" };
            await writer.WriteLineAsync("220 local-test SMTP"); var body = new System.Text.StringBuilder(); var data = false;
            while (await reader.ReadLineAsync(deadline.Token) is { } line)
            {
                if (data) { if (line == ".") { await writer.WriteLineAsync("250 captured"); return body.ToString(); } else body.AppendLine(line); continue; }
                if (line.StartsWith("DATA", StringComparison.Ordinal)) { await writer.WriteLineAsync("354 continue"); data = true; }
                else if (line.StartsWith("QUIT", StringComparison.Ordinal)) { await writer.WriteLineAsync("221 bye"); break; }
                else await writer.WriteLineAsync("250 OK");
            }
            return body.ToString();
        }, deadline.Token);
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Email:Enabled"] = "true", ["Email:Host"] = "127.0.0.1", ["Email:Port"] = ((IPEndPoint)listener.LocalEndpoint).Port.ToString(), ["Email:EnableSsl"] = "false", ["Email:From"] = "payahead@example.test" }).Build();
            var transport = new SmtpPasswordResetEmail(config, NullLogger<SmtpPasswordResetEmail>.Instance);
            Assert.True(await transport.SendAsync("capture@example.test", "exp://127.0.0.1:8085/--/reset-password?token=test-capture-only", 15, deadline.Token));
            var mime = await capture;
            var split = mime.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            Assert.True(split >= 0);
            var body = mime[(split + 4)..];
            if (mime[..split].Contains("Content-Transfer-Encoding: base64", StringComparison.OrdinalIgnoreCase))
                body = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(body));
            else body = System.Text.RegularExpressions.Regex.Replace(body, "=\\r?\\n", "");
            Assert.Contains("PayAhead", body); Assert.Contains("reset-password", body);
        }
        finally { listener.Stop(); }
    }
    private sealed class CaptureEmail : IPasswordResetEmail
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> Links { get; } = new(); public bool Success { get; set; } = true;
        public Task<bool> SendAsync(string address, string link, int minutes, CancellationToken ct) { Links.Enqueue(link); return Task.FromResult(Success); }
    }
}
