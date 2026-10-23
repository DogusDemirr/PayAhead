using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayAhead.Application.Auth;

namespace PayAhead.Infrastructure;

// Bounded, memory-only delivery. Restart can lose queued mail; a new request is safe after the cooldown.
// Raw reset secrets never enter durable queue storage, telemetry or logs.
public sealed class PasswordResetEmailWorker(IServiceScopeFactory scopes, TimeProvider clock, ILogger<PasswordResetEmailWorker> logger)
    : BackgroundService, IPasswordResetDispatcher
{
    private readonly Channel<PasswordResetDelivery> queue = Channel.CreateBounded<PasswordResetDelivery>(new BoundedChannelOptions(100)
    { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });
    public bool TryEnqueue(PasswordResetDelivery delivery)
    {
        var queued = queue.Writer.TryWrite(delivery);
        if (!queued) logger.LogWarning("Password reset email queue is full.");
        return queued;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var item in queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    if (clock.GetUtcNow().UtcDateTime >= item.ExpiresAtUtc) continue;
                    using var scope = scopes.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<IPasswordResetEmail>();
                    if (!await sender.SendAsync(item.Email, item.Link, item.LifetimeMinutes, stoppingToken))
                        await scope.ServiceProvider.GetRequiredService<IPasswordResetStore>().InvalidateAsync(item.TokenId, clock.GetUtcNow().UtcDateTime, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception) { logger.LogWarning("Password reset email processing failed; no secret is logged."); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
