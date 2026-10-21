using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayAhead.Infrastructure;

namespace PayAhead.Api;

public sealed class DatabaseHealthCheck(PayAheadDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken)) return HealthCheckResult.Unhealthy();
            if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any()) return HealthCheckResult.Unhealthy();
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
