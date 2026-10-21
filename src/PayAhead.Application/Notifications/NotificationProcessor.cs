using PayAhead.Domain;

namespace PayAhead.Application.Notifications;

public sealed class NotificationProcessor(INotificationQueue queue, IPushNotificationSender sender, TimeProvider clock)
{
    public async Task<bool> ProcessOneAsync(CancellationToken ct)
    {
        var delivery = await queue.ClaimAsync(Guid.NewGuid(), clock.GetUtcNow().UtcDateTime, ct);
        if (delivery is null) return false;
        PushResult result;
        try
        {
            if (delivery.ProviderMessageId is { } ticket) result = await sender.GetReceiptAsync(ticket, ct);
            else
            {
                var message = await queue.RevalidateAsync(delivery, clock.GetUtcNow().UtcDateTime, ct);
                if (message is null) return true;
                result = await sender.SendAsync(message, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception) { result = new(PushResultKind.Retryable, ErrorCode: "ProviderUnavailable"); }
        await queue.CompleteAsync(delivery, result, clock.GetUtcNow().UtcDateTime, ct);
        Operations.OperationsMetrics.Deliveries.Add(1, new KeyValuePair<string, object?>("result", result.Kind.ToString()));
        return true;
    }
}
