using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayAhead.Application.Auth;

namespace PayAhead.Infrastructure;
public sealed class LocalAvatarStorage(IConfiguration config, IHostEnvironment environment, ILogger<LocalAvatarStorage> logger) : IAvatarStorage
{
    private static readonly Regex SafeKey = new("^[a-f0-9]{32}\\.jpg$", RegexOptions.CultureInvariant);
    private string Root
    {
        get
        {
            var configured = config["Avatars:Root"];
            if (string.IsNullOrWhiteSpace(configured) && !environment.IsDevelopment()) throw new AvatarException("avatar_unavailable");
            var sql = new SqlConnectionStringBuilder(config.GetConnectionString("PayAhead"));
            // A test DB must never sweep a real Development DB's files, even with the same content root.
            var scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql.DataSource + ":" + sql.InitialCatalog)))[..24];
            return Path.GetFullPath(Path.Combine(configured ?? Path.Combine(environment.ContentRootPath, ".data", "avatars"), scope));
        }
    }
    private string FilePath(string key) { if (!SafeKey.IsMatch(key)) throw new AvatarException("avatar_unavailable"); return Path.Combine(Root, key); }
    public async Task<string> WriteAsync(byte[] jpeg, CancellationToken ct)
    {
        var key = Guid.NewGuid().ToString("N") + ".jpg"; var path = FilePath(key);
        try { Directory.CreateDirectory(Root); await using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true)) await file.WriteAsync(jpeg, ct); return key; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or OperationCanceledException)
        { await DeleteAsync(key, CancellationToken.None); if (e is OperationCanceledException) throw; throw new AvatarException("avatar_unavailable"); }
    }
    public async Task<byte[]?> ReadAsync(string key, CancellationToken ct)
    {
        try { return await File.ReadAllBytesAsync(FilePath(key), ct); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw new AvatarException("avatar_unavailable"); }
    }
    public Task<bool> DeleteAsync(string key, CancellationToken ct)
    {
        try { File.Delete(FilePath(key)); return Task.FromResult(true); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { logger.LogWarning("Avatar cleanup deferred. ExceptionType={ExceptionType}", e.GetType().Name); return Task.FromResult(false); }
    }
    public IEnumerable<string> CleanupCandidates(DateTime olderThanUtc)
    {
        if (!Directory.Exists(Root)) return [];
        return Directory.EnumerateFiles(Root, "*.jpg", SearchOption.TopDirectoryOnly).Where(p => SafeKey.IsMatch(Path.GetFileName(p)) && File.GetLastWriteTimeUtc(p) < olderThanUtc).Take(1000).Select(Path.GetFileName).Select(x => x!).ToArray();
    }
}
