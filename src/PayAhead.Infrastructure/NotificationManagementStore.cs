using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Notifications;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Application.Premium;

namespace PayAhead.Infrastructure;

public sealed class NotificationManagementStore(PayAheadDbContext db, TimeProvider clock, IEntitlementService? entitlements = null) : INotificationManagementStore
{
    private IEntitlementService Policy => entitlements ?? new SqlEntitlementService(db, new(), clock);
    private async Task<PaymentPlan> Plan(Guid user, Guid plan, CancellationToken ct, bool locked = false)
        => await (locked ? db.PaymentPlans.FromSqlInterpolated($"SELECT * FROM PaymentPlans WITH (UPDLOCK,HOLDLOCK) WHERE Id={plan} AND UserId={user}")
            : db.PaymentPlans.Where(x => x.Id == plan && x.UserId == user)).SingleOrDefaultAsync(ct) ?? throw new ResourceNotFoundException();
    private async Task LockUser(Guid user, CancellationToken ct)
    { if (await db.Users.FromSqlInterpolated($"SELECT * FROM Users WITH (UPDLOCK,HOLDLOCK) WHERE Id={user} AND IsActive=1").SingleOrDefaultAsync(ct) is null) throw new AuthenticationRequiredException(); }
    public async Task<IReadOnlyList<ReminderRule>> ListRulesAsync(Guid user, Guid plan, CancellationToken ct)
    {
        await Plan(user, plan, ct);
        return await db.ReminderRules.AsNoTracking().Where(x => x.UserId == user && x.PaymentPlanId == plan && !x.IsDeleted && x.Target == ReminderTarget.Payment).OrderBy(x => x.DaysBefore).ThenBy(x => x.TimeOfDay).ToArrayAsync(ct);
    }
    public Task<ReminderRule> WriteRuleAsync(Guid user, Guid plan, Guid? id, ReminderRequest request, CancellationToken ct)
        => Transaction(async () =>
        {
            await Policy.LockUserAsync(user, ct);
            await Plan(user, plan, ct, true);
            var time = TimeOnly.ParseExact(request.TimeOfDay!, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            var rule = id.HasValue ? await db.ReminderRules.SingleOrDefaultAsync(x => x.Id == id && x.UserId == user && x.PaymentPlanId == plan && !x.IsDeleted && x.Target == ReminderTarget.Payment, ct)
                ?? throw new ResourceNotFoundException() : new ReminderRule { Id = Guid.NewGuid(), UserId = user, PaymentPlanId = plan };
            if (await db.ReminderRules.AnyAsync(x => x.PaymentPlanId == plan && !x.IsDeleted && x.Target == ReminderTarget.Payment && x.Id != rule.Id && x.DaysBefore == request.DaysBefore && x.TimeOfDay == time, ct))
                throw new ResourceConflictException("Aynı hatırlatma zaten var.");
            if (!id.HasValue && await db.ReminderRules.CountAsync(x => x.PaymentPlanId == plan && !x.IsDeleted && x.Target == ReminderTarget.Payment, ct) >= NotificationPolicy.MaximumRulesPerPlan)
                throw new ResourceConflictException("Plan başına en fazla 10 hatırlatma olabilir.");
            if (request.IsEnabled && (!id.HasValue || !rule.IsEnabled)) await Policy.EnsureIncreaseAsync(user, LimitResource.Reminders, plan, 1, ct);
            if (!id.HasValue) db.ReminderRules.Add(rule);
            rule.DaysBefore = request.DaysBefore!.Value; rule.TimeOfDay = time; rule.IsEnabled = request.IsEnabled;
            await Cancel(db.NotificationDeliveries.Where(x => x.ReminderRuleId == rule.Id), ct);
            await db.SaveChangesAsync(ct); return rule;
        }, ct);
    public async Task DeleteRuleAsync(Guid user, Guid plan, Guid id, CancellationToken ct)
        => _ = await Transaction(async () =>
        {
            await Plan(user, plan, ct, true);
            var rule = await db.ReminderRules.SingleOrDefaultAsync(x => x.Id == id && x.PaymentPlanId == plan && x.UserId == user && !x.IsDeleted && x.Target == ReminderTarget.Payment, ct) ?? throw new ResourceNotFoundException();
            rule.IsDeleted = true; rule.IsEnabled = false;
            await Cancel(db.NotificationDeliveries.Where(x => x.ReminderRuleId == id), ct);
            await db.SaveChangesAsync(ct); return true;
        }, ct);
    public async Task<IReadOnlyList<DeviceResponse>> ListDevicesAsync(Guid user, CancellationToken ct)
        => await db.DeviceTokens.AsNoTracking().Where(x => x.UserId == user && !x.PushToken.StartsWith("deleted:"))
            .OrderByDescending(x => x.LastSeenAtUtc).Select(x => new DeviceResponse(x.Id, x.Platform, x.IsActive, DateTime.SpecifyKind(x.LastSeenAtUtc, DateTimeKind.Utc))).ToArrayAsync(ct);
    public Task<DeviceResponse> WriteDeviceAsync(Guid user, DeviceRequest request, DateTime now, CancellationToken ct)
        => Transaction(async () =>
        {
            await LockUser(user, ct);
            var existing = await db.DeviceTokens.SingleOrDefaultAsync(x => x.PushToken == request.PushToken, ct);
            if (existing is not null && (existing.UserId != user || request.Id.HasValue && request.Id != existing.Id))
                throw new ResourceConflictException("Cihaz kaydı kullanılamıyor.");
            var device = request.Id.HasValue ? await db.DeviceTokens.SingleOrDefaultAsync(x => x.Id == request.Id && x.UserId == user && !x.PushToken.StartsWith("deleted:"), ct)
                ?? throw new ResourceNotFoundException() : existing;
            if (device is null)
            {
                if (await db.DeviceTokens.CountAsync(x => x.UserId == user && !x.PushToken.StartsWith("deleted:"), ct) >= NotificationPolicy.MaximumDevicesPerUser)
                    throw new ResourceConflictException("En fazla 20 cihaz kaydedilebilir.");
                device = new() { Id = Guid.NewGuid(), UserId = user }; db.DeviceTokens.Add(device);
            }
            if (device.PushToken != request.PushToken || device.Platform != request.Platform)
                await Cancel(db.NotificationDeliveries.Where(x => x.DeviceTokenId == device.Id), ct);
            device.Platform = request.Platform!; device.PushToken = request.PushToken!; device.IsActive = true; device.LastSeenAtUtc = now;
            await db.SaveChangesAsync(ct); return new DeviceResponse(device.Id, device.Platform, true, now);
        }, ct);
    public async Task DeleteDeviceAsync(Guid user, Guid id, DateTime now, CancellationToken ct)
        => _ = await Transaction(async () =>
        {
            await LockUser(user, ct);
            var device = await db.DeviceTokens.SingleOrDefaultAsync(x => x.Id == id && x.UserId == user && !x.PushToken.StartsWith("deleted:"), ct) ?? throw new ResourceNotFoundException();
            device.IsActive = false; device.PushToken = "deleted:" + device.Id.ToString("N"); device.LastSeenAtUtc = now;
            await Cancel(db.NotificationDeliveries.Where(x => x.DeviceTokenId == id), ct);
            await db.SaveChangesAsync(ct); return true;
        }, ct);
    public async Task<SettingsResponse> GetSettingsAsync(Guid user, CancellationToken ct)
        => Response(await db.UserSettings.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == user, ct));
    public Task<SettingsResponse> WriteSettingsAsync(Guid user, SettingsRequest request, DateTime now, CancellationToken ct)
        => Transaction(async () =>
        {
            await LockUser(user, ct);
            var settings = await db.UserSettings.SingleOrDefaultAsync(x => x.UserId == user, ct);
            if (settings is null) { settings = new() { UserId = user, CreatedAtUtc = now }; db.UserSettings.Add(settings); }
            settings.LanguageCode = request.LanguageCode!; settings.DefaultCurrencyCode = request.DefaultCurrencyCode!.Trim().ToUpperInvariant();
            settings.TimeZoneId = request.TimeZoneId!; settings.NotificationsEnabled = request.NotificationsEnabled!.Value; settings.UpdatedAtUtc = now;
            await Cancel(db.NotificationDeliveries.Where(x => x.UserId == user), ct);
            await db.SaveChangesAsync(ct); return Response(settings);
        }, ct);
    private static SettingsResponse Response(UserSettings? settings)
    {
        var zone = NotificationPolicy.Zone(settings?.TimeZoneId, out var fallback);
        return new(settings?.LanguageCode ?? "tr", settings?.DefaultCurrencyCode ?? "TRY", zone.Id, settings?.NotificationsEnabled ?? true, fallback);
    }
    private Task<int> Cancel(IQueryable<NotificationDelivery> query, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return query.Where(x => x.ProviderMessageId == null && (x.Status == DeliveryStatus.Pending || x.Status == DeliveryStatus.Processing))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, DeliveryStatus.Cancelled).SetProperty(x => x.LastErrorCode, "ConfigurationChanged")
                .SetProperty(x => x.UpdatedAtUtc, now).SetProperty(x => x.LeaseOwner, (Guid?)null).SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null), ct);
    }
    private Task<T> Transaction<T>(Func<Task<T>> action, CancellationToken ct)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear(); await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try { var result = await action(); await transaction.CommitAsync(ct); return result; }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            { throw new ResourceConflictException("Aynı kayıt zaten var."); }
        });
}
