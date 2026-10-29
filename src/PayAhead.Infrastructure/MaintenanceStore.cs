using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Operations;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class MaintenanceStore(PayAheadDbContext db, MaintenanceOptions options)
{
    public async Task<int> CleanupAsync(DateTime now, CancellationToken ct)
    {
        // Never remove a family while any member is still inside the reuse-detection window.
        var cutoff = now.AddDays(-options.RefreshRetentionDays);
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var ids = await db.Database.SqlQuery<Guid>($"""
                SELECT TOP ({options.BatchSize}) t.Id AS Value FROM RefreshTokens t WITH(UPDLOCK,READPAST,READCOMMITTEDLOCK)
                WHERE t.ExpiresAtUtc < {cutoff} AND (t.RevokedAtUtc IS NULL OR t.RevokedAtUtc < {cutoff})
                AND NOT EXISTS (SELECT 1 FROM RefreshTokens f WHERE f.FamilyId=t.FamilyId AND
                    (f.ExpiresAtUtc >= {cutoff} OR f.RevokedAtUtc >= {cutoff}))
                AND NOT EXISTS (SELECT 1 FROM RefreshTokens r WHERE r.ReplacedByTokenId=t.Id)
                ORDER BY t.ExpiresAtUtc,t.Id
                """).ToArrayAsync(ct);
            // Leaves are removed first so the self FK remains valid even for long rotation chains.
            var count = await db.RefreshTokens.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync(ct);
            var buckets = await db.RateLimitBuckets.Where(x => x.ExpiresAtUtc < now.AddMinutes(-5)).OrderBy(x => x.ExpiresAtUtc).Take(options.BatchSize).Select(x => x.Id).ToArrayAsync(ct);
            count += await db.RateLimitBuckets.Where(x => buckets.Contains(x.Id) && x.ExpiresAtUtc < now.AddMinutes(-5)).ExecuteDeleteAsync(ct);
            var historyIds = await db.Set<PasswordHistoryEntry>().Where(x => x.NormalChangedAtUtc <= now.AddDays(-30)).OrderBy(x => x.NormalChangedAtUtc).Take(options.BatchSize).Select(x => x.Id).ToArrayAsync(ct);
            count += await db.Set<PasswordHistoryEntry>().Where(x => historyIds.Contains(x.Id) && x.NormalChangedAtUtc <= now.AddDays(-30)).ExecuteUpdateAsync(s => s.SetProperty(x => x.NormalChangedAtUtc, (DateTime?)null), ct);
            await tx.CommitAsync(ct);
            return count;
        });
    }
}
