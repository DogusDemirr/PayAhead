using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class AuthStore(PayAheadDbContext db) : IAuthStore
{
    // Unknown accounts still perform the same expensive Identity verification.
    private static readonly User Dummy = new() { PasswordHash = new IdentityPasswordService().Hash(new User(), "dummy-account-timing-only") };

    public async Task RegisterAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        db.UserSettings.Add(new() { UserId = user.Id, CreatedAtUtc = user.CreatedAtUtc, UpdatedAtUtc = user.CreatedAtUtc });
        try { await db.SaveChangesAsync(ct); } // EF's implicit transaction covers both inserts.
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new ResourceConflictException("Bu e-posta ile kayıt oluşturulamıyor."); }
    }

    public Task<User?> AuthenticateAsync(string email, Func<User, bool> verify, DateTime now, CancellationToken ct)
        => TransactionAsync(async () =>
        {
            var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [NormalizedEmail] = {email}").SingleOrDefaultAsync(ct);
            if (user is null) { verify(Dummy); return null; }
            var valid = verify(user);
            if (!user.IsActive || user.LoginLockedUntilUtc > now) return null;
            if (!valid)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= 5) { user.LoginLockedUntilUtc = now.AddMinutes(15); user.FailedLoginCount = 0; }
                await db.SaveChangesAsync(ct);
                return null;
            }
            user.FailedLoginCount = 0; user.LoginLockedUntilUtc = null; user.UpdatedAtUtc = now;
            await db.SaveChangesAsync(ct);
            return user;
        }, ct);

    public async Task AddSessionAsync(RefreshToken token, CancellationToken ct)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
    }

    public Task<User?> RotateAsync(string hash, RefreshToken replacement, DateTime now, CancellationToken ct)
        => ChangeSessionAsync(hash, replacement, now, ct);
    public async Task RevokeAsync(string hash, DateTime now, CancellationToken ct)
        => _ = await ChangeSessionAsync(hash, null, now, ct);

    private async Task<User?> ChangeSessionAsync(string hash, RefreshToken? replacement, DateTime now, CancellationToken ct)
    {
        // Immutable owner lookup precedes the transaction; every mutation then locks the user first.
        // This serializes a user's rotations/revocations across every API instance, including different family members.
        var owner = await db.RefreshTokens.AsNoTracking().Where(x => x.TokenHash == hash).Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return null;
        return await TransactionAsync(async () =>
        {
            var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {owner.Value}").SingleOrDefaultAsync(ct);
            var old = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
            if (old is null || user is null) return null;
            if (replacement is null || old.RevokedAtUtc is not null || old.ExpiresAtUtc <= now || !user.IsActive)
            {
                await db.RefreshTokens.Where(x => x.UserId == old.UserId && x.FamilyId == old.FamilyId && x.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RevokedAtUtc, now), ct);
                return null; // Commit the revocation before the service produces a generic 401.
            }
            replacement.UserId = old.UserId; replacement.FamilyId = old.FamilyId;
            db.RefreshTokens.Add(replacement);
            await db.SaveChangesAsync(ct); // Self-FK target must exist first, within the same transaction.
            old.RevokedAtUtc = now; old.ReplacedByTokenId = replacement.Id;
            await db.SaveChangesAsync(ct);
            return user;
        }, ct);
    }

    public Task<User?> FindActiveAsync(Guid id, CancellationToken ct)
        => db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

    private Task<User?> TransactionAsync(Func<Task<User?>> action, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        });
}
