using System.Text;
using PayAhead.Application;
using PayAhead.Application.Auth;
using PayAhead.Infrastructure;
using SkiaSharp;
using Xunit;

namespace PayAhead.UnitTests;
public sealed class AvatarTests
{
    public static byte[] Picture(int width = 32, int height = 24)
    {
        using var bitmap = new SKBitmap(width, height); bitmap.Erase(SKColors.Teal);
        using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Png, 100); return encoded.ToArray();
    }
    [Fact] public void Real_pixels_are_cropped_resized_and_reencoded_without_source_metadata()
    {
        var jpeg = new AvatarImageProcessor().Normalize(Picture());
        using var codec = SKCodec.Create(new MemoryStream(jpeg)); Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat); Assert.Equal(512, codec.Info.Width); Assert.Equal(512, codec.Info.Height);
        var marker = Encoding.ASCII.GetBytes("GPS-LOCATION-PRIVATE");
        var comment = new byte[] { 0xff, 0xfe, 0, (byte)(marker.Length + 2) }.Concat(marker).ToArray();
        var withMetadata = jpeg[..2].Concat(comment).Concat(jpeg[2..]).ToArray();
        var clean = new AvatarImageProcessor().Normalize(withMetadata); Assert.DoesNotContain("GPS-LOCATION-PRIVATE", Encoding.ASCII.GetString(clean));
    }
    [Theory][InlineData("<svg xmlns='http://www.w3.org/2000/svg'/>")][InlineData("MZ executable")][InlineData("not an image")]
    public void Extensions_and_declared_mime_cannot_make_non_images_valid(string input) => Assert.Equal("avatar_invalid", Assert.Throws<AvatarException>(() => new AvatarImageProcessor().Normalize(Encoding.UTF8.GetBytes(input))).Code);
    [Fact] public void Byte_and_pixel_limits_are_enforced_before_decoding_large_pixels()
    {
        Assert.Equal("avatar_too_large", Assert.Throws<AvatarException>(() => new AvatarImageProcessor().Normalize(new byte[AvatarService.MaximumBytes + 1])).Code);
        Assert.Equal("avatar_too_large", Assert.Throws<AvatarException>(() => new AvatarImageProcessor().Normalize(Picture(2049, 1))).Code);
    }
    [Theory][InlineData(true, false)][InlineData(false, true)]
    public async Task Failed_commit_cleanup_never_removes_a_referenced_photo(bool referenced, bool shouldDelete)
    {
        var storage = new StorageStub(); var service = new AvatarService(new StoreStub(referenced), storage, new AvatarImageProcessor(), new Owner());
        var bytes = Picture();
        await Assert.ThrowsAsync<AvatarException>(() => service.UploadAsync(new MemoryStream(bytes), bytes.Length, Convert.ToBase64String(new byte[8]), CancellationToken.None));
        Assert.Equal(shouldDelete, storage.Deleted);
    }
    private sealed class Owner : ICurrentUser { public Guid? UserId => Guid.Parse("aaaaaaaa-aaaa-4aaa-baaa-aaaaaaaaaaaa"); }
    private sealed class StoreStub(bool referenced) : IAvatarStore
    {
        public Task<bool> IsReferencedAsync(string key, CancellationToken ct) => Task.FromResult(referenced);
        public Task<byte[]> ReadAsync(Guid user, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProfileResponse> ReplaceAsync(Guid user, string? key, byte[] version, CancellationToken ct) => throw new AvatarException("avatar_unavailable");
    }
    private sealed class StorageStub : IAvatarStorage
    {
        public bool Deleted { get; private set; }
        public Task<string> WriteAsync(byte[] data, CancellationToken ct) => Task.FromResult(new string('a', 32) + ".jpg");
        public Task<byte[]?> ReadAsync(string key, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string key, CancellationToken ct) { Deleted = true; return Task.FromResult(true); }
        public IEnumerable<string> CleanupCandidates(DateTime olderThanUtc) => [];
    }
}
