using System.Text.Json.Serialization;
using PayAhead.Domain;

namespace PayAhead.Application.Notifications;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ReminderRequest(int? DaysBefore, string? TimeOfDay = "09:00", bool IsEnabled = true);
public sealed record ReminderResponse(Guid Id, Guid PaymentPlanId, int DaysBefore, string TimeOfDay, bool IsEnabled)
{
    public static ReminderResponse From(ReminderRule rule) => new(rule.Id, rule.PaymentPlanId, rule.DaysBefore, rule.TimeOfDay.ToString("HH:mm"), rule.IsEnabled);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DeviceRequest(string? Platform, string? PushToken, Guid? Id = null);
// Push token is deliberately never echoed; client keeps the platform token locally.
public sealed record DeviceResponse(Guid Id, string Platform, bool IsActive, DateTime LastSeenAtUtc);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SettingsRequest(string? LanguageCode, string? DefaultCurrencyCode, string? TimeZoneId, bool? NotificationsEnabled);
public sealed record SettingsResponse(string LanguageCode, string DefaultCurrencyCode, string TimeZoneId, bool NotificationsEnabled, bool UsedTimeZoneFallback);

public interface INotificationManagementStore
{
    Task<IReadOnlyList<ReminderRule>> ListRulesAsync(Guid user, Guid plan, CancellationToken ct);
    Task<ReminderRule> WriteRuleAsync(Guid user, Guid plan, Guid? id, ReminderRequest request, CancellationToken ct);
    Task DeleteRuleAsync(Guid user, Guid plan, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DeviceResponse>> ListDevicesAsync(Guid user, CancellationToken ct);
    Task<DeviceResponse> WriteDeviceAsync(Guid user, DeviceRequest request, DateTime now, CancellationToken ct);
    Task DeleteDeviceAsync(Guid user, Guid id, DateTime now, CancellationToken ct);
    Task<SettingsResponse> GetSettingsAsync(Guid user, CancellationToken ct);
    Task<SettingsResponse> WriteSettingsAsync(Guid user, SettingsRequest request, DateTime now, CancellationToken ct);
}
public interface INotificationManagementService
{
    Task<IReadOnlyList<ReminderResponse>> ListRulesAsync(Guid plan, CancellationToken ct);
    Task<ReminderResponse> WriteRuleAsync(Guid plan, Guid? id, ReminderRequest request, CancellationToken ct);
    Task DeleteRuleAsync(Guid plan, Guid id, CancellationToken ct);
    Task<IReadOnlyList<DeviceResponse>> ListDevicesAsync(CancellationToken ct);
    Task<DeviceResponse> WriteDeviceAsync(DeviceRequest request, CancellationToken ct);
    Task DeleteDeviceAsync(Guid id, CancellationToken ct);
    Task<SettingsResponse> GetSettingsAsync(CancellationToken ct);
    Task<SettingsResponse> WriteSettingsAsync(SettingsRequest request, CancellationToken ct);
}

public enum PushResultKind { TicketAccepted, ReceiptAccepted, PendingReceipt, Retryable, Permanent, InvalidDevice, Resend }
public sealed record PushResult(PushResultKind Kind, string? MessageId = null, string? ErrorCode = null, TimeSpan? RetryAfter = null);
public sealed record PushMessage(Guid DeliveryId, string PushToken, Guid? PaymentPlanId = null, Guid? SubscriptionId = null, string LanguageCode = "tr");
public interface IPushNotificationSender
{
    Task<PushResult> SendAsync(PushMessage message, CancellationToken ct);
    Task<PushResult> GetReceiptAsync(string messageId, CancellationToken ct);
}
public interface INotificationQueue
{
    Task<Guid?> ScheduleBatchAsync(Guid? afterRule, DateTime now, CancellationToken ct);
    Task<NotificationDelivery?> ClaimAsync(Guid leaseOwner, DateTime now, CancellationToken ct);
    Task<PushMessage?> RevalidateAsync(NotificationDelivery delivery, DateTime now, CancellationToken ct);
    Task CompleteAsync(NotificationDelivery delivery, PushResult result, DateTime now, CancellationToken ct);
    Task<int> CleanupAsync(DateTime now, CancellationToken ct);
}
