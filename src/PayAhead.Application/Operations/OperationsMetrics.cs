using System.Diagnostics.Metrics;

namespace PayAhead.Application.Operations;

public static class OperationsMetrics
{
    public static readonly Meter Meter = new("PayAhead.Processing", "1.0");
    public static readonly Counter<long> Deliveries = Meter.CreateCounter<long>("payahead.notification.results");
    public static readonly Counter<long> Webhooks = Meter.CreateCounter<long>("payahead.webhook.results");
}
