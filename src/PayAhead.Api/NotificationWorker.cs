using PayAhead.Application.Notifications;
using PayAhead.Infrastructure;

namespace PayAhead.Api;

public sealed class NotificationWorker(IServiceScopeFactory scopes, NotificationOptions options,
    TimeProvider clock, ILogger<NotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled) { logger.LogInformation("Notification worker disabled; no push is sent."); return; }
        Guid? cursor = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var queue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();
                cursor = await queue.ScheduleBatchAsync(cursor, clock.GetUtcNow().UtcDateTime, stoppingToken);
                var processor = scope.ServiceProvider.GetRequiredService<NotificationProcessor>();
                var processed = 0;
                while (processed < 50 && await processor.ProcessOneAsync(stoppingToken)) processed++;
                var cleaned = await queue.CleanupAsync(clock.GetUtcNow().UtcDateTime, stoppingToken);
                logger.LogInformation("Notification cycle completed. Processed={Processed} Cleaned={Cleaned}", processed, cleaned);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // Provider exceptions may contain URLs or tokens: log only the exception type.
                logger.LogError("Notification cycle failed. ExceptionType={ExceptionType}", ex.GetType().Name);
            }
            try { await Task.Delay(TimeSpan.FromSeconds(options.PollSeconds), clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
