using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Operations;

namespace PayAhead.Infrastructure;

public sealed class SqlSharedRateLimiter(PayAheadDbContext db) : ISharedRateLimiter
{
    public async Task<bool> AcquireAsync(string key, int limit, CancellationToken ct)
    {
        // SQL server time avoids API-node clock skew. The HMAC key already includes the policy.
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                var resource = "PayAhead.Rate:" + key;
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @result int;
                    EXEC @result=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=3000;
                    IF @result<0 THROW 51000, 'Rate limiter unavailable', 1;
                    """, ct);
                var row = await db.RateLimitBuckets.FromSqlInterpolated($"SELECT * FROM RateLimitBuckets WITH(UPDLOCK,HOLDLOCK,INDEX(PK_RateLimitBuckets)) WHERE Id={key}").SingleOrDefaultAsync(ct);
                var now = await db.Database.SqlQuery<DateTime>($"SELECT SYSUTCDATETIME() AS Value").SingleAsync(ct);
                if (row is null) { row = new() { Id = key, ExpiresAtUtc = now.AddMinutes(1) }; db.RateLimitBuckets.Add(row); }
                else if (row.ExpiresAtUtc <= now) { row.Count = 0; row.ExpiresAtUtc = now.AddMinutes(1); }
                var accepted = row.Count < limit;
                if (accepted) { row.Count++; await db.SaveChangesAsync(ct); }
                await tx.CommitAsync(ct);
                return accepted;
            });
        }
        catch (Exception ex) when (ex is SqlException or DbUpdateException or TimeoutException or Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException || ex is InvalidOperationException { InnerException: SqlException })
        { throw new OperationalUnavailableException(); }
    }
}
