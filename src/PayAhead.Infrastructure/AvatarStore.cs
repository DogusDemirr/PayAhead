using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Application.Payments;

namespace PayAhead.Infrastructure;
public sealed class AvatarStore(PayAheadDbContext db, IAvatarStorage storage, TimeProvider clock) : IAvatarStore
{
    public Task<bool> IsReferencedAsync(string key, CancellationToken ct) => db.Users.AsNoTracking().AnyAsync(x => x.AvatarKey == key, ct);
    public async Task<byte[]> ReadAsync(Guid user, CancellationToken ct)
    {
        var row = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user && x.IsActive, ct) ?? throw new AuthenticationRequiredException();
        if (row.AvatarKey is null) throw new ResourceNotFoundException();
        return await storage.ReadAsync(row.AvatarKey, ct) ?? throw new ResourceNotFoundException();
    }
    public async Task<ProfileResponse> ReplaceAsync(Guid user, string? key, byte[] expectedVersion, CancellationToken ct)
    {
        string? old = null;
        var result = await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct);
            var row = await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH(UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) ?? throw new AuthenticationRequiredException();
            if (!row.RowVersion.SequenceEqual(expectedVersion)) throw new AvatarException("avatar_changed");
            old = row.AvatarKey; row.AvatarKey = key; row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return new ProfileResponse(row.Id, row.Email, row.DisplayName, Convert.ToBase64String(row.RowVersion)) { HasAvatar = key is not null };
        });
        if (old is not null && old != key) await storage.DeleteAsync(old, CancellationToken.None);
        return result;
    }
}
