using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;
namespace PayAhead.IntegrationTests;
public sealed partial class AuthApiTests
{
    [Fact] public async Task Registration_rejects_missing_or_stale_confirmations_without_partial_writes()
    {
        RegistrationLegal?[] invalid = [null, TestLegal.Confirmed with { NoticeAcknowledged = false }, TestLegal.Confirmed with { PrivacyAcknowledged = false }, TestLegal.Confirmed with { TermsAccepted = false }, TestLegal.Confirmed with { NoticeVersion = null }, TestLegal.Confirmed with { PrivacyVersion = "stale" }, TestLegal.Confirmed with { TermsVersion = "stale" }];
        foreach (var legal in invalid)
        {
            var result = await client.PostAsJsonAsync("/api/auth/register", Registration() with { Legal = legal }); Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("application/problem+json", result.Content.Headers.ContentType?.MediaType); Assert.DoesNotContain(Registration().Password!, await result.Content.ReadAsStringAsync());
        }
        await using var db = Db(); Assert.Empty(await db.Users.ToArrayAsync()); Assert.Empty(await db.Set<RegistrationLegalConfirmation>().ToArrayAsync()); Assert.Empty(await db.Set<LegalNoticeReceipt>().ToArrayAsync());
    }
    [Fact] public async Task Registration_persists_three_distinct_confirmation_types_versions_and_server_utc_atomically()
    {
        var auth = await RegisterLogin(); Bearer(auth.AccessToken);
        await using var db = Db(); var rows = await db.Set<RegistrationLegalConfirmation>().AsNoTracking().OrderBy(x => x.DocumentId).ToArrayAsync();
        Assert.Equal(3, rows.Length); Assert.All(rows, row => { Assert.Equal(auth.User.Id, row.UserId); Assert.Equal(LegalDocuments.Version, row.DocumentVersion); Assert.Equal((db.Users.Single()).CreatedAtUtc, row.ConfirmedAtUtc); });
        Assert.Equal(LegalConfirmationKind.NoticeAcknowledgment, rows.Single(x => x.DocumentId == "notice").Kind);
        Assert.Equal(LegalConfirmationKind.PrivacyReadAcknowledgment, rows.Single(x => x.DocumentId == "privacy").Kind);
        Assert.Equal(LegalConfirmationKind.TermsAcceptance, rows.Single(x => x.DocumentId == "terms").Kind);
        var export = await client.GetFromJsonAsync<JsonElement>("/api/users/me/export"); Assert.Equal(3, export.GetProperty("registrationLegal").GetArrayLength());
        var deleted = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/users/me") { Content = JsonContent.Create(new { currentPassword = Registration().Password }) }); Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await db.Set<RegistrationLegalConfirmation>().AsNoTracking().ToArrayAsync());
    }
    [Fact] public async Task Existing_account_can_sign_in_without_retroactive_confirmations_and_keeps_old_receipt()
    {
        await using var db = Db(); var time = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var old = new User { Id = Guid.NewGuid(), Email = "legacy@example.com", NormalizedEmail = "LEGACY@EXAMPLE.COM", DisplayName = "Legacy", CreatedAtUtc = time, UpdatedAtUtc = time }; old.PasswordHash = new IdentityPasswordService().Hash(old, Registration().Password!); var hash = old.PasswordHash;
        await new AuthStore(db).RegisterAsync(old, default, "legacy-version"); var auth = await Login("legacy@example.com"); Assert.Equal(old.Id, auth.User.Id);
        var receipt = await db.Set<LegalNoticeReceipt>().AsNoTracking().SingleAsync(); Assert.Equal("legacy-version", receipt.NoticeVersion); Assert.Equal(time, receipt.PresentedAtUtc);
        Assert.Empty(await db.Set<RegistrationLegalConfirmation>().ToArrayAsync()); Assert.Equal(hash, (await db.Users.AsNoTracking().SingleAsync()).PasswordHash);
    }
}
