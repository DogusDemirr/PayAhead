using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Domain;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed partial class AuthApiTests
{
    [Fact]
    public async Task Legal_manifest_and_registration_record_presentation_without_claiming_consent()
    {
        var manifest = await client.GetFromJsonAsync<JsonElement>("/api/legal");
        Assert.Equal(LegalDocuments.Version, manifest.GetProperty("version").GetString());
        Assert.True(manifest.GetProperty("isDraft").GetBoolean()); Assert.False(manifest.GetProperty("requiresTermsAcceptance").GetBoolean());
        var stale = await client.PostAsJsonAsync("/api/auth/register", Registration() with { LegalVersion = "obsolete" });
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration() with { LegalVersion = LegalDocuments.Version })).StatusCode);
        await using var db = Db(); var receipt = await db.Set<LegalNoticeReceipt>().SingleAsync(); var user = await db.Users.SingleAsync();
        Assert.Equal(user.Id, receipt.UserId); Assert.Equal(LegalDocuments.Version, receipt.NoticeVersion); Assert.Equal(user.CreatedAtUtc, receipt.PresentedAtUtc);
        Assert.Empty(await db.Set<PasswordHistoryEntry>().ToArrayAsync());
    }

    [Fact]
    public async Task Security_http_enforces_three_changes_and_returns_safe_limit_and_owner_summary()
    {
        var first = await RegisterLogin(); Bearer(first.AccessToken);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/auth/register", Registration("second@example.com"))).StatusCode);
        var current = Registration().Password!;
        for (var i = 1; i <= 3; i++)
        {
            var summary = await client.GetFromJsonAsync<SecuritySummary>("/api/users/me/security"); Assert.Equal(4 - i, summary!.RemainingChanges);
            var next = $"http-new-password-{i}!";
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest(current, next, next))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me/security")).StatusCode);
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("first@example.com", next)); Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Bearer((await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken); current = next;
        }
        var limited = await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest(current, "http-fourth-password!", "http-fourth-password!"));
        Assert.Equal(HttpStatusCode.Conflict, limited.StatusCode); Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
        Assert.Equal("password_change_limit", (await limited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        var exhausted = await client.GetFromJsonAsync<SecuritySummary>("/api/users/me/security"); Assert.Equal(0, exhausted!.RemainingChanges); Assert.NotNull(exhausted.NextAvailableAtUtc); Assert.Equal(1, exhausted.ActiveSessions);
        var other = await Login("second@example.com"); Bearer(other.AccessToken); Assert.Equal(3, (await client.GetFromJsonAsync<SecuritySummary>("/api/users/me/security"))!.RemainingChanges);
    }

    [Fact]
    public async Task Reused_and_common_password_http_failures_do_not_consume_allowance_or_expose_secrets()
    {
        var auth = await RegisterLogin(); Bearer(auth.AccessToken);
        var same = await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest(Registration().Password, Registration().Password, Registration().Password));
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);
        Assert.True((await same.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
        await using (var db = Db())
        {
            var user = await db.Users.SingleAsync();
            db.Set<PasswordHistoryEntry>().Add(new() { Id = Guid.NewGuid(), UserId = user.Id, PasswordHash = new PayAhead.Infrastructure.IdentityPasswordService().Hash(user, "retired-http-password!") });
            await db.SaveChangesAsync();
        }
        foreach (var (candidate, code) in new[] { ("retired-http-password!", "password_reused"), ("password12345", "password_common") })
        {
            var result = await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest(Registration().Password, candidate, candidate));
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode); var body = await result.Content.ReadAsStringAsync(); Assert.DoesNotContain(candidate, body);
            Assert.Equal(code, JsonDocument.Parse(body).RootElement.GetProperty("code").GetString());
        }
        Assert.Equal(3, (await client.GetFromJsonAsync<SecuritySummary>("/api/users/me/security"))!.RemainingChanges);
    }
}
