using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Auth;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed class AuthApiTests(SqlServerFixture sqlServer) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private string connection = "";
    private readonly string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;
    private PayAheadDbContext Db() => new(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).Options);
    public async Task InitializeAsync()
    {
        connection = new SqlConnectionStringBuilder(sqlServer.ConnectionString) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var db = Db(); await db.Database.MigrateAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseRegressionPremium()
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("Jwt:SigningKey", key)
            .UseSetting("DevelopmentIdentity:Enabled", "false"));
        client = factory.CreateClient();
    }
    public async Task DisposeAsync()
    {
        client.Dispose(); await factory.DisposeAsync(); await using var db = Db();
        Assert.StartsWith("PayAheadTest_", db.Database.GetDbConnection().Database); await db.Database.EnsureDeletedAsync();
    }
    private static RegisterRequest Registration(string email = "first@example.com") => new(email, "correct-long-password!", "Auth user");
    private async Task<AuthResponse> Login(string email = "first@example.com")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "correct-long-password!"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("no-store,no-cache", response.Headers.CacheControl?.ToString()?.Replace(" ", ""));
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
    private async Task<AuthResponse> RegisterLogin(string email = "first@example.com")
    {
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration(email))).StatusCode);
        return await Login(email);
    }
    private void Bearer(string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact] public async Task Registration_persists_hash_and_settings_without_automatic_login()
    {
        var result = await client.PostAsJsonAsync("/api/auth/register", Registration(" First@Example.com "));
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        var body = await result.Content.ReadAsStringAsync(); Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        await using var db = Db(); var user = await db.Users.SingleAsync();
        Assert.Equal("FIRST@EXAMPLE.COM", user.NormalizedEmail); Assert.NotEqual(Registration().Password, user.PasswordHash);
        Assert.True(new IdentityPasswordService().Verify(user, Registration().Password!, out _));
        Assert.Equal(user.Id, (await db.UserSettings.SingleAsync()).UserId); Assert.Empty(await db.RefreshTokens.ToArrayAsync());
    }
    [Fact] public async Task Concurrent_duplicate_registration_has_one_winner_and_no_orphan_settings()
    {
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/auth/register", Registration()), client.PostAsJsonAsync("/api/auth/register", Registration("FIRST@example.com")));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = Db(); Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(1, await db.UserSettings.CountAsync());
    }
    [Theory]
    [InlineData("", "correct-long-password!", "Name")]
    [InlineData("bad", "correct-long-password!", "Name")]
    [InlineData("user@example.com", "", "Name")]
    [InlineData("user@example.com", "short", "Name")]
    [InlineData("user@example.com", "correct-long-password!", " ")]
    public async Task Registration_invalid_input_returns_safe_400(string email, string password, string name)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, name));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("correct-long-password!", await response.Content.ReadAsStringAsync());
    }
    [Fact] public async Task Login_failure_is_generic_and_persistent_account_lock_blocks_correct_password()
    {
        await client.PostAsJsonAsync("/api/auth/register", Registration());
        var missing = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("missing@example.com", "wrong"));
        var a = await missing.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        for (var i = 0; i < 5; i++)
        {
            var wrong = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("first@example.com", "wrong"));
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            var b = await wrong.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(a.GetProperty("title").GetString(), b.GetProperty("title").GetString());
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("first@example.com", Registration().Password))).StatusCode);
        await using var db = Db(); Assert.True((await db.Users.SingleAsync()).LoginLockedUntilUtc > DateTime.UtcNow);
        await db.Users.ExecuteUpdateAsync(x => x.SetProperty(u => u.LoginLockedUntilUtc, DateTime.UtcNow.AddSeconds(-1)));
        await Login();
    }
    [Fact] public async Task Rotation_reuse_revokes_family_but_other_login_session_survives_and_logout_is_idempotent()
    {
        var first = await RegisterLogin(); var independent = await Login();
        var result = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken)); Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var rotated = (await result.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.NotEqual(first.RefreshToken, rotated.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(first.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(rotated.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(independent.RefreshToken))).StatusCode);
        var logoutSession = await Login();
        for (var i = 0; i < 2; i++) Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(logoutSession.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(logoutSession.RefreshToken))).StatusCode);
        Bearer(logoutSession.AccessToken); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        await using var db = Db(); var tokens = await db.RefreshTokens.ToArrayAsync();
        Assert.All(tokens, x => Assert.Equal(64, x.TokenHash.Length)); Assert.DoesNotContain(tokens, x => x.TokenHash == first.RefreshToken);
        Assert.All(tokens.Where(x => x.ReplacedByTokenId is not null), chain => Assert.Equal(chain.FamilyId, tokens.Single(x => x.Id == chain.ReplacedByTokenId).FamilyId));
    }
    [Fact] public async Task Concurrent_refresh_only_one_wins_and_reuse_invalidates_winning_child()
    {
        var login = await RegisterLogin();
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken)), client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken)));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Unauthorized);
        var winner = (await responses.Single(x => x.IsSuccessStatusCode).Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(winner.RefreshToken))).StatusCode);
        await using var db = Db(); Assert.Equal(2, await db.RefreshTokens.CountAsync()); Assert.All(await db.RefreshTokens.ToArrayAsync(), x => Assert.NotNull(x.RevokedAtUtc));
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Expired_or_revoked_refresh_is_rejected(bool revoked)
    {
        var login = await RegisterLogin(); await using var db = Db();
        if (revoked) await db.RefreshTokens.ExecuteUpdateAsync(x => x.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow));
        else await db.RefreshTokens.ExecuteUpdateAsync(x => x.SetProperty(t => t.CreatedAtUtc, DateTime.UtcNow.AddDays(-2)).SetProperty(t => t.ExpiresAtUtc, DateTime.UtcNow.AddDays(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken))).StatusCode);
    }
    [Fact] public async Task Inactive_and_deleted_users_cannot_use_access_or_login_or_refresh()
    {
        var login = await RegisterLogin(); await using var db = Db(); await db.Users.ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, false));
        Bearer(login.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/payments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("first@example.com", Registration().Password))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(login.RefreshToken))).StatusCode);
        await db.RefreshTokens.ExecuteDeleteAsync(); await db.UserSettings.ExecuteDeleteAsync(); await db.Users.ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }
    [Theory]
    [InlineData("missing")] [InlineData("signature")] [InlineData("issuer")] [InlineData("audience")]
    [InlineData("expired")] [InlineData("algorithm")] [InlineData("subject")] [InlineData("unknown-user")] [InlineData("malformed")]
    public async Task Jwt_validation_rejects_invalid_credentials(string scenario)
    {
        var login = await RegisterLogin();
        if (scenario != "missing")
        {
            var now = DateTime.UtcNow; var claims = new[] { new Claim("sub", scenario == "subject" ? "not-guid" : scenario == "unknown-user" ? Guid.NewGuid().ToString() : login.User.Id.ToString()) };
            var signingKey = scenario == "signature" ? RandomNumberGenerator.GetBytes(64) : Convert.FromBase64String(key);
            var token = new JwtSecurityToken(scenario == "issuer" ? "Wrong" : "PayAhead", scenario == "audience" ? "Wrong" : "PayAhead.Mobile", claims,
                now.AddHours(-1), scenario == "expired" ? now.AddMinutes(-1) : now.AddMinutes(10),
                new SigningCredentials(new SymmetricSecurityKey(signingKey), scenario == "algorithm" ? SecurityAlgorithms.HmacSha512 : SecurityAlgorithms.HmacSha256));
            Bearer(scenario == "malformed" ? "invalid.jwt.value" : new JwtSecurityTokenHandler().WriteToken(token));
        }
        var result = await client.GetAsync("/api/payments"); Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType);
    }
    [Fact] public async Task Real_jwt_isolates_payments_occurrences_upcoming_dashboard_and_calendar()
    {
        var first = await RegisterLogin(); Bearer(first.AccessToken); var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var result = await client.PostAsJsonAsync("/api/payments", new { name = "private", amount = 12.3456m, currencyCode = "TRY", category = "Bills", frequency = "Once", interval = 1, startDate = date });
        Assert.Equal(HttpStatusCode.Created, result.StatusCode); var plan = await result.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(); var id = plan.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        var second = await RegisterLogin("second@example.com"); Bearer(second.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/payments/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/payments/{id}/occurrences")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/payments/{id}/occurrences/{date}/status", new { status = "Paid", actualAmount = 12m })).StatusCode);
        var upcoming = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/payments/upcoming"); Assert.Equal(0, upcoming.GetProperty("totalCount").GetInt32());
        var summary = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/dashboard/summary"); Assert.Equal(0, summary.GetProperty("activePaymentPlanCount").GetInt32());
        var calendar = await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/calendar?year={DateTime.UtcNow.Year}&month={DateTime.UtcNow.Month}"); Assert.Equal(0, calendar.GetProperty("days").GetArrayLength());
    }
    [Fact] public async Task Rate_limit_returns_429_and_spoofed_forwarded_IP_cannot_bypass_it()
    {
        for (var i = 0; i < 20; i++) { client.DefaultRequestHeaders.Remove("X-Forwarded-For"); client.DefaultRequestHeaders.Add("X-Forwarded-For", $"192.0.2.{i}"); Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""))).StatusCode); }
        var blocked = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("", ""));
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode); Assert.NotNull(blocked.Headers.RetryAfter);
    }
    [Fact] public async Task Shipping_api_never_uses_development_identity_even_when_enabled()
    {
        await using var shipping = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development").UseRegressionPremium()
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "true")
            .UseSetting("DevelopmentIdentity:UserId", Guid.NewGuid().ToString()));
        using var http = shipping.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/payments")).StatusCode);
    }
    [Theory] [InlineData(null)] [InlineData("weak")] [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void Production_startup_refuses_missing_or_weak_secret(string? secret)
    {
        using var shipping = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Production")
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false").UseSetting("Jwt:SigningKey", secret ?? ""));
        var error = Assert.ThrowsAny<Exception>(() => shipping.CreateClient()); Assert.Contains("JWT signing key", error.ToString());
    }
    [Fact] public async Task Production_redirects_plain_HTTP_to_explicit_HTTPS_port()
    {
        await using var shipping = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Production")
            .UseSetting("ConnectionStrings:PayAhead", connection).UseSetting("DevelopmentIdentity:Enabled", "false").UseSetting("Jwt:SigningKey", key));
        using var http = shipping.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await http.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.Equal("https", response.Headers.Location?.Scheme); Assert.Equal(443, response.Headers.Location?.Port);
    }
    [Fact] public async Task Rotation_rolls_back_child_insert_when_parent_update_fails()
    {
        var login = await RegisterLogin();
        var interceptor = new FailSecondSave();
        await using var failingDb = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(connection).AddInterceptors(interceptor).Options);
        var now = DateTime.UtcNow;
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(login.RefreshToken)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new AuthStore(failingDb).RotateAsync(hash,
            new RefreshToken { Id = Guid.NewGuid(), TokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw))), CreatedAtUtc = now, ExpiresAtUtc = now.AddDays(30) }, now, CancellationToken.None));
        await using var db = Db(); var persisted = await db.RefreshTokens.SingleAsync(); Assert.Null(persisted.RevokedAtUtc); Assert.Null(persisted.ReplacedByTokenId);
    }
    private sealed class FailSecondSave : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        private int count;
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (++count == 2) throw new InvalidOperationException("Injected transaction failure");
            return ValueTask.FromResult(result);
        }
    }
    [Fact] public async Task Migration_preserves_legacy_records_and_revokes_unknown_refresh_lineage()
    {
        // Separate test-owned DB: start at Phase 1, seed existing user/token, then upgrade.
        var legacyConnection = new SqlConnectionStringBuilder(connection) { InitialCatalog = "PayAheadTest_" + Guid.NewGuid().ToString("N") }.ConnectionString;
        await using var legacy = new PayAheadDbContext(new DbContextOptionsBuilder<PayAheadDbContext>().UseSqlServer(legacyConnection).Options);
        try
        {
            await legacy.GetService<IMigrator>().MigrateAsync("20261021084857_InitialCreate");
            var id = Guid.NewGuid(); var tokenId = Guid.NewGuid();
            await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (Id,Email,NormalizedEmail,DisplayName,PasswordHash,IsActive,CreatedAtUtc,UpdatedAtUtc) VALUES ({id},'legacy@example.com','LEGACY@EXAMPLE.COM','Legacy','hash',1,SYSUTCDATETIME(),SYSUTCDATETIME())");
            await legacy.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO RefreshTokens (Id,UserId,TokenHash,CreatedAtUtc,ExpiresAtUtc) VALUES ({tokenId},{id},{new string('A',64)},SYSUTCDATETIME(),DATEADD(day,1,SYSUTCDATETIME()))");
            await legacy.Database.MigrateAsync(); Assert.Equal(id, (await legacy.Users.SingleAsync()).Id);
            var token = await legacy.RefreshTokens.SingleAsync(); Assert.Equal(tokenId, token.FamilyId); Assert.NotNull(token.RevokedAtUtc);
            Assert.Empty(await legacy.Database.GetPendingMigrationsAsync());
        }
        finally { Assert.StartsWith("PayAheadTest_", legacy.Database.GetDbConnection().Database); await legacy.Database.EnsureDeletedAsync(); }
    }
}
