using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class ProfileStore(PayAheadDbContext db, IPasswordService passwords, TimeProvider clock) : IProfileStore
{
    private static ProfileResponse Response(User user) => new(user.Id, user.Email, user.DisplayName, Convert.ToBase64String(user.RowVersion)) { HasAvatar = user.AvatarKey is not null };
    public async Task<ProfileResponse> ReadAsync(Guid user, CancellationToken ct) => Response(await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user && x.IsActive, ct) ?? throw new AuthenticationRequiredException());
    public Task<ProfileResponse> UpdateAsync(Guid user, UpdateProfileRequest request, CancellationToken ct) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await Owner(user, ct);
        if (Convert.ToBase64String(row.RowVersion) != request.RowVersion) throw new ResourceConflictException("Profile changed.");
        row.DisplayName = request.DisplayName!.Trim(); row.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return Response(row);
    });
    public Task ChangePasswordAsync(Guid user, ChangePasswordRequest request, CancellationToken ct) => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await Owner(user, ct);
        if (!passwords.Verify(row, request.CurrentPassword!, out _)) throw new CurrentPasswordException();
        var now = clock.GetUtcNow().UtcDateTime;
        row.PasswordHash = passwords.Hash(row, request.NewPassword!); row.SecurityStamp = Guid.NewGuid(); row.FailedLoginCount = 0; row.LoginLockedUntilUtc = null; row.UpdatedAtUtc = now;
        await db.RefreshTokens.Where(x => x.UserId == user && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), ct);
        await db.DeviceTokens.Where(x => x.UserId == user && x.IsActive).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);
        await db.Set<PasswordResetToken>().Where(x => x.UserId == user && x.ConsumedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    });
    private async Task<User> Owner(Guid user, CancellationToken ct) => await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH(UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) ?? throw new AuthenticationRequiredException();
}
