using System.Text.Json.Serialization;

namespace PayAhead.Application.Operations;

public interface ISharedRateLimiter
{
    Task<bool> AcquireAsync(string key, int limit, CancellationToken ct);
}
public interface IAccountStore
{
    Task<byte[]> ExportAsync(Guid user, CancellationToken ct);
    Task DeleteAsync(Guid user, string password, CancellationToken ct);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DeleteAccountRequest(string? CurrentPassword);
public sealed class OperationalUnavailableException : Exception;
public sealed class ExportLimitException : Exception;
public sealed class MaintenanceOptions
{
    public bool Enabled { get; init; } = true;
    public int RefreshRetentionDays { get; init; } = 120;
    public int BatchSize { get; init; } = 200;
}
