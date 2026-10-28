using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayAhead.Application.Auth;

namespace PayAhead.Infrastructure;
public sealed class AvatarCleanupWorker(IServiceScopeFactory scopes, IAvatarStorage files, TimeProvider clock, ILogger<AvatarCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await SweepAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch { logger.LogWarning("Avatar orphan cleanup deferred."); }
        }
    }
    public async Task SweepAsync(CancellationToken ct)
    {
        // Grace period fences writes whose metadata transaction has not committed yet.
        var candidates = files.CleanupCandidates(clock.GetUtcNow().UtcDateTime.AddHours(-1)).ToArray();
        if (candidates.Length == 0) return;
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PayAheadDbContext>();
        var live = await db.Users.AsNoTracking().Where(x => x.AvatarKey != null && candidates.Contains(x.AvatarKey)).Select(x => x.AvatarKey!).ToArrayAsync(ct);
        foreach (var key in candidates.Except(live, StringComparer.Ordinal)) await files.DeleteAsync(key, ct);
    }
}
