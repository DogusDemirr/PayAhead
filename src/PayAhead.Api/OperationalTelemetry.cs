using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayAhead.Application.Operations;
using PayAhead.Infrastructure;

namespace PayAhead.Api;

public static class OperationalTelemetry
{
    public static readonly Meter Meter = new("PayAhead", "1.0");
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("payahead.requests");
    public static readonly Counter<long> Outcomes = Meter.CreateCounter<long>("payahead.outcomes");
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("payahead.request.duration", "ms");
    public static readonly ActivitySource Activities = new("PayAhead");
}
public sealed class WorkerStatus(TimeProvider clock, NotificationOptions notifications, StoreOptions stores, MaintenanceOptions maintenance) : IHealthCheck
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTimeOffset> success = new();
    public void Beat(string worker, bool ok, int count = 0)
    {
        if (ok) success[worker] = clock.GetUtcNow();
        OperationalTelemetry.Outcomes.Add(count == 0 ? 1 : count, new KeyValuePair<string, object?>("category", worker), new("result", ok ? "success" : "failure"));
    }
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var required = new List<string>();
        if (notifications.Enabled) required.Add("notification");
        if (stores.WorkerEnabled) required.Add("store");
        if (maintenance.Enabled) required.Add("maintenance");
        return Task.FromResult(required.Any(x => !success.TryGetValue(x, out var time) || clock.GetUtcNow() - time > TimeSpan.FromMinutes(10)) ? HealthCheckResult.Unhealthy() : HealthCheckResult.Healthy());
    }
}
public sealed class MaintenanceWorker(IServiceScopeFactory scopes, MaintenanceOptions options, WorkerStatus health, TimeProvider clock, ILogger<MaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Enabled) return;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var count = await scope.ServiceProvider.GetRequiredService<MaintenanceStore>().CleanupAsync(clock.GetUtcNow().UtcDateTime, ct);
                health.Beat("maintenance", true, count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) { health.Beat("maintenance", false); logger.LogError("Maintenance failed. ExceptionType={ExceptionType}", ex.GetType().Name); }
            try { await Task.Delay(TimeSpan.FromMinutes(1), clock, ct); } catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
        }
    }
}
