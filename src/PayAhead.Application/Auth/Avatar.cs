namespace PayAhead.Application.Auth;

public sealed class AvatarException(string code) : Exception { public string Code { get; } = code; }
public interface IAvatarImageProcessor { byte[] Normalize(byte[] input); }
public interface IAvatarStorage
{
    Task<string> WriteAsync(byte[] jpeg, CancellationToken ct);
    Task<byte[]?> ReadAsync(string key, CancellationToken ct);
    Task<bool> DeleteAsync(string key, CancellationToken ct);
    IEnumerable<string> CleanupCandidates(DateTime olderThanUtc);
}
public interface IAvatarStore
{
    Task<bool> IsReferencedAsync(string key, CancellationToken ct);
    Task<ProfileResponse> ReplaceAsync(Guid user, string? key, byte[] expectedVersion, CancellationToken ct);
    Task<byte[]> ReadAsync(Guid user, CancellationToken ct);
}
public sealed class AvatarService(IAvatarStore store, IAvatarStorage storage, IAvatarImageProcessor images, ICurrentUser current)
{
    public const int MaximumBytes = 2 * 1024 * 1024;
    private Guid User => current.UserId ?? throw new PayAhead.Application.Payments.AuthenticationRequiredException();
    private static byte[] Version(string? token) => PayAhead.Application.Payments.RowVersionToken.IsValid(token) ? Convert.FromBase64String(token!) : throw new AvatarException("avatar_changed");
    public Task<byte[]> ReadAsync(CancellationToken ct) => store.ReadAsync(User, ct);
    public Task<ProfileResponse> DeleteAsync(string? version, CancellationToken ct) => store.ReplaceAsync(User, null, Version(version), ct);
    public async Task<ProfileResponse> UploadAsync(Stream input, long length, string? version, CancellationToken ct)
    {
        var user = User; var expected = Version(version);
        if (length <= 0) throw new AvatarException("avatar_invalid");
        if (length > MaximumBytes) throw new AvatarException("avatar_too_large");
        using var data = new MemoryStream(); var buffer = new byte[65536]; int read;
        while ((read = await input.ReadAsync(buffer, ct)) > 0) { if (data.Length + read > MaximumBytes) throw new AvatarException("avatar_too_large"); await data.WriteAsync(buffer.AsMemory(0, read), ct); }
        var jpeg = images.Normalize(data.ToArray());
        var key = await storage.WriteAsync(jpeg, ct);
        try { return await store.ReplaceAsync(user, key, expected, ct); }
        catch
        {
            // A transient commit acknowledgement failure may occur after the DB committed.
            // Never remove a possibly committed file; the delayed sweeper handles unknown outcomes.
            try { if (!await store.IsReferencedAsync(key, CancellationToken.None)) await storage.DeleteAsync(key, CancellationToken.None); }
            catch { /* Keep the file until the database can confirm it is unreferenced. */ }
            throw;
        }
    }
}
