using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Notifications;
using PayAhead.Domain;
using PayAhead.Domain.Recurrence;

namespace PayAhead.Infrastructure;

public sealed class SqlNotificationQueue(PayAheadDbContext db, IPaymentRecurrenceCalculator recurrence) : INotificationQueue
{
    public async Task<Guid?> ScheduleBatchAsync(Guid? afterRule, DateTime now, CancellationToken ct)
    {
        var rules = db.ReminderRules.AsNoTracking().Where(x => !x.IsDeleted && x.IsEnabled);
        var earliestTrialEnd = DateOnly.FromDayNumber(Math.Max(0, DateOnly.FromDateTime(now).DayNumber - 2));
        rules = rules.Where(x => x.Target == ReminderTarget.Payment || db.TrackedSubscriptions.Any(s => s.PaymentPlanId == x.PaymentPlanId
            && s.UserId == x.UserId && !s.IsDeleted && s.EnableTrialReminders && s.Status == SubscriptionStatus.Trial && s.TrialEndDate >= earliestTrialEnd));
        if (afterRule.HasValue) rules = rules.Where(x => x.Id.CompareTo(afterRule.Value) > 0);
        var ids = await rules.OrderBy(x => x.Id).Take(20).Select(x => x.Id).ToArrayAsync(ct);
        foreach (var id in ids) await ScheduleRule(id, now, ct);
        return ids.Length == 20 ? ids[^1] : null;
    }
    private Task ScheduleRule(Guid id, DateTime now, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            var ruleInfo = await db.ReminderRules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (ruleInfo is null) return;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var plan = await db.PaymentPlans.FromSqlInterpolated($"SELECT * FROM PaymentPlans WITH (UPDLOCK,HOLDLOCK) WHERE Id={ruleInfo.PaymentPlanId} AND UserId={ruleInfo.UserId}").SingleOrDefaultAsync(ct);
            var rule = await db.ReminderRules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (plan is null || rule is null || !rule.IsEnabled || rule.IsDeleted
                || !await db.Users.AnyAsync(x => x.Id == rule.UserId && x.IsActive, ct)) return;
            var settings = await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == rule.UserId, ct);
            if (settings?.NotificationsEnabled == false) return;
            var zone = NotificationPolicy.Zone(settings?.TimeZoneId, out _);
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
            var from = DateOnly.FromDayNumber(Math.Max(0, today.DayNumber - 2));
            var to = DateOnly.FromDayNumber(Math.Min(DateOnly.MaxValue.DayNumber, today.DayNumber + 32 + rule.DaysBefore));
            IReadOnlyList<DateOnly> dates;
            if (rule.Target == ReminderTarget.SubscriptionTrial)
            {
                var subscription = await db.TrackedSubscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentPlanId == plan.Id && x.UserId == rule.UserId, ct);
                dates = subscription?.TrialEndDate is { } end && subscription.TrialReminderEligible(today, end) ? [end] : [];
            }
            else dates = recurrence.CalculateOccurrences(plan, from, to);
            var devices = await db.DeviceTokens.AsNoTracking().Where(x => x.UserId == rule.UserId && x.IsActive).OrderBy(x => x.Id).Take(20).Select(x => x.Id).ToArrayAsync(ct);
            if (devices.Length == 0) return;
            var statuses = await db.PaymentOccurrences.AsNoTracking().Where(x => x.PaymentPlanId == plan.Id && x.DueDate >= from && x.DueDate <= to)
                .ToDictionaryAsync(x => x.DueDate, x => x.Status, ct);
            var existing = await db.NotificationDeliveries.Where(x => x.ReminderRuleId == id && x.DueDate >= from && x.DueDate <= to && x.DeviceTokenId != null && devices.Contains(x.DeviceTokenId.Value))
                .ToDictionaryAsync(x => (x.DueDate, x.DeviceTokenId!.Value), ct);
            foreach (var due in dates)
            {
                if (rule.Target == ReminderTarget.Payment && statuses.TryGetValue(due, out var status) && status != OccurrenceStatus.Pending) continue;
                var scheduled = NotificationPolicy.ScheduledAt(due, rule.DaysBefore, rule.TimeOfDay, zone);
                if (scheduled is null || scheduled < now.AddHours(-24) || scheduled > now.AddDays(30)) continue;
                foreach (var device in devices)
                {
                    if (existing.TryGetValue((due, device), out var delivery))
                    {
                        // Never reset terminal failures, accepted tickets or a live lease during a periodic scan.
                        if (delivery.ProviderMessageId is null && delivery.AttemptCount < 5 && delivery.Status is DeliveryStatus.Pending or DeliveryStatus.Cancelled)
                        {
                            delivery.ScheduledAtUtc = scheduled.Value; delivery.Status = DeliveryStatus.Pending;
                            if (delivery.AttemptCount == 0) delivery.NextAttemptAtUtc = scheduled;
                            delivery.UpdatedAtUtc = now;
                        }
                    }
                    else db.NotificationDeliveries.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        UserId = rule.UserId,
                        PaymentPlanId = plan.Id,
                        ReminderRuleId = id,
                        DeviceTokenId = device,
                        DueDate = due,
                        ScheduledAtUtc = scheduled.Value,
                        NextAttemptAtUtc = scheduled,
                        UpdatedAtUtc = now
                    });
                }
            }
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        });

    public async Task<NotificationDelivery?> ClaimAsync(Guid leaseOwner, DateTime now, CancellationToken ct)
    {
        // UPDATE ... OUTPUT is one atomic SQL statement; UPDLOCK serializes competing claims.
        // READCOMMITTEDLOCK also supports databases with READ_COMMITTED_SNAPSHOT enabled.
        var rows = await db.NotificationDeliveries.FromSqlInterpolated($"""
            ;WITH candidate AS (
                SELECT TOP (1) * FROM NotificationDeliveries WITH (UPDLOCK,READPAST,READCOMMITTEDLOCK)
                WHERE DeviceTokenId IS NOT NULL AND
                  ((Status IN (0,4) AND COALESCE(NextAttemptAtUtc,ScheduledAtUtc) <= {now})
                    OR (Status=1 AND LeaseExpiresAtUtc <= {now}))
                ORDER BY COALESCE(NextAttemptAtUtc,ScheduledAtUtc),Id
            )
            UPDATE candidate SET Status=1, LeaseOwner={leaseOwner}, LeaseExpiresAtUtc={now.AddMinutes(2)},
                AttemptCount=AttemptCount+CASE WHEN ProviderMessageId IS NULL THEN 1 ELSE 0 END,
                ReceiptAttemptCount=ReceiptAttemptCount+CASE WHEN ProviderMessageId IS NULL THEN 0 ELSE 1 END,
                LastAttemptAtUtc={now}, UpdatedAtUtc={now}
            OUTPUT inserted.*;
            """).AsNoTracking().ToListAsync(ct);
        return rows.SingleOrDefault();
    }

    public async Task<PushMessage?> RevalidateAsync(NotificationDelivery delivery, DateTime now, CancellationToken ct)
    {
        var plan = await db.PaymentPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == delivery.PaymentPlanId && x.UserId == delivery.UserId, ct);
        var rule = await db.ReminderRules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == delivery.ReminderRuleId && x.PaymentPlanId == delivery.PaymentPlanId && x.UserId == delivery.UserId, ct);
        var device = await db.DeviceTokens.AsNoTracking().SingleOrDefaultAsync(x => x.Id == delivery.DeviceTokenId && x.UserId == delivery.UserId, ct);
        var settings = await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == delivery.UserId, ct);
        var status = await db.PaymentOccurrences.AsNoTracking().Where(x => x.PaymentPlanId == delivery.PaymentPlanId && x.DueDate == delivery.DueDate).Select(x => (OccurrenceStatus?)x.Status).SingleOrDefaultAsync(ct);
        var active = await db.Users.AnyAsync(x => x.Id == delivery.UserId && x.IsActive, ct);
        var eligible = plan is not null && rule is not null && device is not null
            && NotificationPolicy.Eligible(plan, rule, active, settings?.NotificationsEnabled ?? true, device.IsActive, delivery.DueDate, status, recurrence);
        Guid? subscriptionId = null;
        if (rule?.Target == ReminderTarget.SubscriptionTrial)
        {
            var subscription = await db.TrackedSubscriptions.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentPlanId == delivery.PaymentPlanId && x.UserId == delivery.UserId, ct);
            subscriptionId = subscription?.Id;
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, NotificationPolicy.Zone(settings?.TimeZoneId, out _)));
            eligible = subscription is not null && subscription.TrialReminderEligible(today, delivery.DueDate)
                && active && settings?.NotificationsEnabled != false && device?.IsActive == true && rule.IsEnabled && !rule.IsDeleted;
        }
        var scheduled = rule is null ? null : NotificationPolicy.ScheduledAt(delivery.DueDate, rule.DaysBefore, rule.TimeOfDay, NotificationPolicy.Zone(settings?.TimeZoneId, out _));
        if (plan is null || rule is null || device is null || !NotificationPolicy.ValidToken(device.PushToken)
            || !eligible
            || scheduled is null || scheduled < now.AddHours(-24) || delivery.AttemptCount > 5)
        {
            await OwnedLease(delivery).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, DeliveryStatus.Cancelled)
                .SetProperty(x => x.LastErrorCode, "Suppressed").SetProperty(x => x.LeaseOwner, (Guid?)null).SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null).SetProperty(x => x.UpdatedAtUtc, now), ct);
            return null;
        }
        if (scheduled > now || scheduled != delivery.ScheduledAtUtc)
        {
            await OwnedLease(delivery).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, DeliveryStatus.Pending)
                .SetProperty(x => x.ScheduledAtUtc, scheduled.Value).SetProperty(x => x.NextAttemptAtUtc, scheduled)
                .SetProperty(x => x.LeaseOwner, (Guid?)null).SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null).SetProperty(x => x.UpdatedAtUtc, now), ct);
            return null;
        }
        // A configuration mutation may have revoked the lease during the reads above.
        if (!await OwnedLease(delivery).AnyAsync(x => x.LeaseExpiresAtUtc > now, ct)) return null;
        return new(delivery.Id, device.PushToken, plan.Id, subscriptionId);
    }
    private IQueryable<NotificationDelivery> OwnedLease(NotificationDelivery delivery)
        => db.NotificationDeliveries.Where(x => x.Id == delivery.Id && x.Status == DeliveryStatus.Processing && x.LeaseOwner == delivery.LeaseOwner);

    public Task CompleteAsync(NotificationDelivery delivery, PushResult result, DateTime now, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var row = await db.NotificationDeliveries.FromSqlInterpolated($"SELECT * FROM NotificationDeliveries WITH (UPDLOCK,HOLDLOCK) WHERE Id={delivery.Id}").SingleAsync(ct);
            if (row.Status != DeliveryStatus.Processing || row.LeaseOwner != delivery.LeaseOwner || row.LeaseExpiresAtUtc <= now) return;
            row.LeaseOwner = null; row.LeaseExpiresAtUtc = null; row.UpdatedAtUtc = now;
            var retryDelay = result.RetryAfter is { } requested
                ? TimeSpan.FromSeconds(Math.Clamp(requested.TotalSeconds, 1, 3600)) : NotificationPolicy.Backoff(row.AttemptCount);
            // Error text is never persisted: only a bounded application-defined code.
            row.LastErrorCode = SafeCode(result.ErrorCode);
            switch (result.Kind)
            {
                case PushResultKind.TicketAccepted when !string.IsNullOrWhiteSpace(result.MessageId) && result.MessageId.Length <= 128:
                    row.ProviderMessageId = result.MessageId; row.TicketAcceptedAtUtc = now;
                    row.Status = DeliveryStatus.AwaitingReceipt; row.NextAttemptAtUtc = now.AddMinutes(15); break;
                case PushResultKind.ReceiptAccepted:
                    row.Status = DeliveryStatus.Sent; row.SentAtUtc = now; row.NextAttemptAtUtc = null; break;
                case PushResultKind.PendingReceipt:
                case PushResultKind.Retryable when row.ProviderMessageId is not null:
                    if (row.TicketAcceptedAtUtc <= now.AddHours(-24) || row.ReceiptAttemptCount >= 96)
                    { row.Status = DeliveryStatus.Failed; row.LastErrorCode = "ReceiptExpired"; row.NextAttemptAtUtc = null; }
                    else { row.Status = DeliveryStatus.AwaitingReceipt; row.NextAttemptAtUtc = now.Add(result.RetryAfter is null ? TimeSpan.FromMinutes(15) : retryDelay); }
                    break;
                case PushResultKind.Retryable when row.AttemptCount < 5:
                case PushResultKind.Resend when row.AttemptCount < 5:
                    row.ProviderMessageId = null; row.TicketAcceptedAtUtc = null;
                    row.Status = DeliveryStatus.Pending; row.NextAttemptAtUtc = now.Add(retryDelay); break;
                default: row.Status = DeliveryStatus.Failed; row.NextAttemptAtUtc = null; break;
            }
            if (result.Kind == PushResultKind.InvalidDevice)
                await db.DeviceTokens.Where(x => x.Id == row.DeviceTokenId && x.UserId == row.UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct);
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        });
    private static string? SafeCode(string? code) => code switch
    {
        "DeviceNotRegistered" or "MessageRateExceeded" or "MessageTooBig" or "InvalidCredentials" or "MismatchSenderId"
            or "ProviderUnavailable" or "ProviderRejected" or "ProviderProtocol" or "NotConfigured" or "ReceiptExpired" => code,
        null => null,
        _ => "ProviderRejected"
    };
    public Task<int> CleanupAsync(DateTime now, CancellationToken ct)
        // Preserve 400 days of terminal dedup keys: longer than the 365-day lead + 30-day horizon + catch-up.
        => db.NotificationDeliveries.Where(x => (x.Status == DeliveryStatus.Sent || x.Status == DeliveryStatus.Failed || x.Status == DeliveryStatus.Cancelled)
                && x.UpdatedAtUtc < now.AddDays(-400) && x.DueDate < DateOnly.FromDateTime(now.AddDays(-400)))
            .OrderBy(x => x.UpdatedAtUtc).Take(500).ExecuteDeleteAsync(ct);
}
