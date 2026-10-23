using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayAhead.Application.Auth;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class PasswordResetConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("PasswordResetTokens"); b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique(); b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class PasswordResetStore(PayAheadDbContext db) : IPasswordResetStore
{
    public Task<User?> IssueAsync(string normalizedEmail, PasswordResetToken token, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct);
            var user = await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH(UPDLOCK,HOLDLOCK) WHERE NormalizedEmail={normalizedEmail}").SingleOrDefaultAsync(ct);
            if (user is null || !user.IsActive) { await tx.CommitAsync(ct); return null; }
            if (await db.Set<PasswordResetToken>().AnyAsync(x => x.UserId == user.Id && x.CreatedAtUtc > token.CreatedAtUtc.AddMinutes(-2), ct))
            { await tx.CommitAsync(ct); return null; }
            await db.Set<PasswordResetToken>().Where(x => x.UserId == user.Id && x.ConsumedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, token.CreatedAtUtc), ct);
            // Bound retained token rows without touching any financial data.
            await db.Set<PasswordResetToken>().Where(x => x.UserId == user.Id && x.ExpiresAtUtc < token.CreatedAtUtc.AddDays(-1)).ExecuteDeleteAsync(ct);
            token.UserId = user.Id; db.Add(token); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return user;
        });
    public async Task<bool> ConsumeAsync(string hash, DateTime now, Func<User, string> newHash, CancellationToken ct)
    {
        var owner = await db.Set<PasswordResetToken>().AsNoTracking().Where(x => x.TokenHash == hash).Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(ct);
        if (owner is null) return false;
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var tx = await db.Database.BeginTransactionAsync(ct);
            var user = await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH(UPDLOCK,HOLDLOCK) WHERE Id={owner.Value}").SingleOrDefaultAsync(ct);
            var token = await db.Set<PasswordResetToken>().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
            if (user is null || !user.IsActive || token is null || token.ConsumedAtUtc != null || token.ExpiresAtUtc <= now) return false;
            user.PasswordHash = newHash(user); user.FailedLoginCount = 0; user.LoginLockedUntilUtc = null; user.UpdatedAtUtc = now;
            await db.Set<PasswordResetToken>().Where(x => x.UserId == user.Id && x.ConsumedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
            await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), ct);
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
        });
    }
    public Task InvalidateAsync(Guid id, DateTime now, CancellationToken ct) => db.Set<PasswordResetToken>().Where(x => x.Id == id)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
}
