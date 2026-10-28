using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using PayAhead.Application.Auth;
using PayAhead.Infrastructure;
using SkiaSharp;
using Xunit;

namespace PayAhead.IntegrationTests;
public sealed partial class AuthApiTests
{
    private static byte[] AvatarPicture()
    {
        using var bitmap = new SKBitmap(24, 32); bitmap.Erase(SKColors.Teal); using var image = SKImage.FromBitmap(bitmap); using var bytes = image.Encode(SKEncodedImageFormat.Png, 100); return bytes.ToArray();
    }
    private static HttpRequestMessage AvatarPut(byte[] bytes, string version, bool injectedOwner = false)
    {
        var body = new MultipartFormDataContent(); var file = new ByteArrayContent(bytes); file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        body.Add(file, "image", "../../unsafe.svg"); if (injectedOwner) body.Add(new StringContent(Guid.NewGuid().ToString()), "userId");
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/users/me/avatar") { Content = body }; request.Headers.IfMatch.Add(new EntityTagHeaderValue('"' + version + '"')); return request;
    }
    private async Task<ProfileResponse> AvatarProfile() => (await client.GetFromJsonAsync<ProfileResponse>("/api/users/me/profile"))!;
    private async Task<HttpResponseMessage> RemoveAvatar(string version) { var request = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me/avatar"); request.Headers.IfMatch.Add(new EntityTagHeaderValue('"' + version + '"')); return await client.SendAsync(request); }
    [Fact] public async Task Avatar_is_private_persistent_replaced_and_removed_with_safe_server_keys()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me/avatar")).StatusCode);
        var auth = await RegisterLogin(); Bearer(auth.AccessToken); var initial = await AvatarProfile(); Assert.False(initial.HasAvatar);
        var upload = await client.SendAsync(AvatarPut(AvatarPicture(), initial.RowVersion)); Assert.Equal(HttpStatusCode.OK, upload.StatusCode); var saved = (await upload.Content.ReadFromJsonAsync<ProfileResponse>())!; Assert.True(saved.HasAvatar);
        var raw = await upload.Content.ReadAsStringAsync(); Assert.DoesNotContain("AvatarKey", raw, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain(".jpg", raw);
        await using var db = Db(); var oldKey = (await db.Users.AsNoTracking().SingleAsync()).AvatarKey!; Assert.Matches("^[a-f0-9]{32}\\.jpg$", oldKey);
        var download = await client.GetAsync("/api/users/me/avatar"); Assert.Equal(HttpStatusCode.OK, download.StatusCode); Assert.Equal("image/jpeg", download.Content.Headers.ContentType!.MediaType); Assert.True(download.Headers.CacheControl!.NoStore);
        using (var image = SKCodec.Create(new MemoryStream(await download.Content.ReadAsByteArrayAsync()))) { Assert.Equal(512, image.Info.Width); Assert.Equal(512, image.Info.Height); }
        Bearer((await Login()).AccessToken); var restored = await AvatarProfile(); Assert.True(restored.HasAvatar); Assert.True((await client.GetFromJsonAsync<UserResponse>("/api/users/me"))!.HasAvatar);
        var replaced = await client.SendAsync(AvatarPut(AvatarPicture(), restored.RowVersion)); Assert.Equal(HttpStatusCode.OK, replaced.StatusCode); var current = (await replaced.Content.ReadFromJsonAsync<ProfileResponse>())!;
        var files = factory.Services.GetRequiredService<IAvatarStorage>(); Assert.Null(await files.ReadAsync(oldKey, CancellationToken.None));
        var newKey = (await db.Users.AsNoTracking().SingleAsync()).AvatarKey!; Assert.NotEqual(oldKey, newKey);
        var orphan = await files.WriteAsync(new AvatarImageProcessor().Normalize(AvatarPicture()), CancellationToken.None);
        var sweeper = new AvatarCleanupWorker(factory.Services.GetRequiredService<IServiceScopeFactory>(), files, new FutureClock(), NullLogger<AvatarCleanupWorker>.Instance);
        await sweeper.SweepAsync(CancellationToken.None);
        Assert.Null(await files.ReadAsync(orphan, CancellationToken.None)); Assert.NotNull(await files.ReadAsync(newKey, CancellationToken.None));
        Assert.Equal(HttpStatusCode.Conflict, (await RemoveAvatar(restored.RowVersion)).StatusCode); Assert.True((await AvatarProfile()).HasAvatar);
        Assert.Equal(HttpStatusCode.OK, (await RemoveAvatar(current.RowVersion)).StatusCode); Assert.False((await AvatarProfile()).HasAvatar); Assert.Null(await files.ReadAsync(newKey, CancellationToken.None)); Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/users/me/avatar")).StatusCode);
    }
    [Fact] public async Task Avatar_rejects_non_images_large_files_and_cross_user_versions_without_replacing_existing_photo()
    {
        var owner = await RegisterLogin(); var other = await RegisterLogin("other@example.com"); Bearer(owner.AccessToken); var initial = await AvatarProfile();
        var uploaded = await client.SendAsync(AvatarPut(AvatarPicture(), initial.RowVersion)); Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode); var saved = (await uploaded.Content.ReadFromJsonAsync<ProfileResponse>())!;
        var originalBytes = await client.GetByteArrayAsync("/api/users/me/avatar");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(AvatarPut(Encoding.UTF8.GetBytes("<svg/>"), saved.RowVersion))).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.SendAsync(AvatarPut(new byte[AvatarService.MaximumBytes + 1], saved.RowVersion))).StatusCode);
        Bearer(other.AccessToken); Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(AvatarPut(AvatarPicture(), saved.RowVersion))).StatusCode); Assert.False((await AvatarProfile()).HasAvatar);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(AvatarPut(AvatarPicture(), saved.RowVersion, true))).StatusCode);
        Bearer(owner.AccessToken); Assert.Equal(originalBytes, await client.GetByteArrayAsync("/api/users/me/avatar")); Assert.Equal(HttpStatusCode.OK, (await RemoveAvatar(saved.RowVersion)).StatusCode);
    }
    [Fact] public async Task Storage_failure_preserves_previous_photo_and_account_delete_cleans_the_file()
    {
        var auth = await RegisterLogin(); Bearer(auth.AccessToken); var initial = await AvatarProfile();
        var uploaded = await client.SendAsync(AvatarPut(AvatarPicture(), initial.RowVersion)); Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode); var saved = (await uploaded.Content.ReadFromJsonAsync<ProfileResponse>())!;
        var original = await client.GetByteArrayAsync("/api/users/me/avatar");
        await using var failing = factory.WithWebHostBuilder(b => b.ConfigureServices(s => { s.RemoveAll<IAvatarStorage>(); s.AddSingleton<IAvatarStorage, FailingAvatarStorage>(); }));
        using var http = failing.CreateClient(); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await http.SendAsync(AvatarPut(AvatarPicture(), saved.RowVersion))).StatusCode); Assert.Equal(original, await client.GetByteArrayAsync("/api/users/me/avatar"));
        await using var db = Db(); var key = (await db.Users.AsNoTracking().SingleAsync()).AvatarKey!;
        var deletion = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me") { Content = JsonContent.Create(new { currentPassword = "correct-long-password!" }) }; Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(deletion)).StatusCode);
        Assert.Null(await factory.Services.GetRequiredService<IAvatarStorage>().ReadAsync(key, CancellationToken.None)); Assert.Null((await db.Users.AsNoTracking().SingleAsync()).AvatarKey);
    }
    [Fact] public async Task Avatar_write_rate_limit_is_per_verified_user_and_keeps_existing_photo()
    {
        var owner = await RegisterLogin(); var other = await RegisterLogin("other@example.com"); Bearer(owner.AccessToken); var profile = await AvatarProfile();
        for (var i = 0; i < 5; i++) Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(AvatarPut(Encoding.UTF8.GetBytes("not an image"), profile.RowVersion))).StatusCode);
        var limited = await client.SendAsync(AvatarPut(AvatarPicture(), profile.RowVersion)); Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode); Assert.NotNull(limited.Headers.RetryAfter);
        Assert.False((await AvatarProfile()).HasAvatar);
        Bearer(other.AccessToken); var foreign = await AvatarProfile(); Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(AvatarPut(AvatarPicture(), foreign.RowVersion))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RemoveAvatar((await AvatarProfile()).RowVersion)).StatusCode);
    }
    private sealed class FailingAvatarStorage : IAvatarStorage
    {
        public Task<string> WriteAsync(byte[] jpeg, CancellationToken ct) => throw new AvatarException("avatar_unavailable");
        public Task<byte[]?> ReadAsync(string key, CancellationToken ct) => Task.FromResult<byte[]?>(null);
        public Task<bool> DeleteAsync(string key, CancellationToken ct) => Task.FromResult(true);
        public IEnumerable<string> CleanupCandidates(DateTime olderThanUtc) => [];
    }
    private sealed class FutureClock : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddHours(2); }
}
