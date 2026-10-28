using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.IntegrationTests;

public sealed partial class AuthApiTests
{
    [Fact] public async Task Profile_is_scoped_validated_and_has_real_rowversion_concurrency()
    {
        var first = await RegisterLogin(); var second = await RegisterLogin("second@example.com"); Bearer(first.AccessToken);
        var original = (await client.GetFromJsonAsync<ProfileResponse>("/api/users/me/profile"))!;
        Assert.Equal(first.User.Id, original.Id); Assert.Equal(8, Convert.FromBase64String(original.RowVersion).Length);
        var update = new UpdateProfileRequest("  New name  ", original.RowVersion);
        var response = await client.PutAsJsonAsync("/api/users/me/profile", update); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<ProfileResponse>())!; Assert.Equal("New name", saved.DisplayName); Assert.Equal(original.Email, saved.Email); Assert.NotEqual(original.RowVersion, saved.RowVersion);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/users/me/profile", update)).StatusCode);
        foreach (var name in new[] { "", "  ", new string('a', 101), "invalid\nname" }) Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/users/me/profile", new UpdateProfileRequest(name, saved.RowVersion))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/users/me/profile", new { displayName = "Hacker", rowVersion = saved.RowVersion, userId = second.User.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/users/me/profile", new { displayName = "Hacker", rowVersion = saved.RowVersion, email = "other@example.com" })).StatusCode);
        Assert.Equal("New name", (await client.GetFromJsonAsync<UserResponse>("/api/users/me"))!.DisplayName);
        Bearer(second.AccessToken); Assert.Equal(second.User.DisplayName, (await client.GetFromJsonAsync<ProfileResponse>("/api/users/me/profile"))!.DisplayName);
        client.DefaultRequestHeaders.Authorization = null; Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me/profile")).StatusCode);
    }
    [Fact] public async Task Password_change_revokes_all_access_refresh_and_fences_late_login_without_touching_finance()
    {
        var first = await RegisterLogin(); var otherSession = await Login(); Bearer(first.AccessToken);
        await using var db = Db(); var owner = await db.Users.AsNoTracking().SingleAsync(x => x.Id == first.User.Id);
        db.DeviceTokens.Add(new() { Id = Guid.NewGuid(), UserId = owner.Id, Platform = "android", PushToken = "ExpoPushToken[profile-fixture-only]", LastSeenAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync();
        var plan = new PaymentPlan { Id = Guid.NewGuid(), UserId = owner.Id, Name = "Private plan", Amount = 12.3456m, StartDate = new(2026, 10, 28), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow }; db.PaymentPlans.Add(plan); await db.SaveChangesAsync(); var planVersion = plan.RowVersion.ToArray();
        var wrong = await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest("incorrect-password", "new-correct-password!", "new-correct-password!")); Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode); Assert.Contains("current_password_invalid", await wrong.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest("correct-long-password!", "new-correct-password!", "new-correct-password!"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode); Bearer(otherSession.AccessToken); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        foreach (var token in new[] { first.RefreshToken, otherSession.RefreshToken }) Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(token))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(first.User.Email, "correct-long-password!"))).StatusCode);
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(first.User.Email, "new-correct-password!")); Assert.Equal(HttpStatusCode.OK, login.StatusCode); Bearer((await login.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => new AuthStore(db).AddSessionAsync(new RefreshToken { Id = Guid.NewGuid(), UserId = owner.Id, FamilyId = Guid.NewGuid(), TokenHash = new string('A', 64), CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.AddDays(1) }, CancellationToken.None, owner.SecurityStamp));
        db.ChangeTracker.Clear(); var after = await db.PaymentPlans.SingleAsync(); Assert.Equal(12.3456m, after.Amount); Assert.Equal(planVersion, after.RowVersion); Assert.False((await db.DeviceTokens.SingleAsync()).IsActive);
    }
    [Fact] public async Task Password_policy_and_credentials_rate_limit_do_not_mutate_the_account()
    {
        var auth = await RegisterLogin(); Bearer(auth.AccessToken);
        for (var i = 0; i < 5; i++)
        {
            var invalid = await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest("correct-long-password!", "short", "different"));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode); var body = await invalid.Content.ReadAsStringAsync(); Assert.DoesNotContain("correct-long-password!", body); Assert.DoesNotContain("'short'", body);
        }
        var limited = await client.PostAsJsonAsync("/api/users/me/change-password", new ChangePasswordRequest("correct-long-password!", "new-correct-password!", "new-correct-password!")); Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);
    }
}
