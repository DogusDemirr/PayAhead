namespace PayAhead.Domain;

public enum PaymentFrequency { Once, Daily, Weekly, Monthly, Yearly }
public enum OccurrenceStatus { Pending, Paid, Skipped }
public enum DeliveryStatus { Pending, Processing, Sent, Failed }

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed partial class PaymentPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public string Category { get; set; } = "Other";
    public PaymentFrequency Frequency { get; set; }
    public int Interval { get; set; } = 1;
    public DateOnly StartDate { get; set; }
    public int? AnchorDay { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed partial class PaymentOccurrence
{
    public Guid Id { get; set; }
    public Guid PaymentPlanId { get; set; }
    public DateOnly DueDate { get; set; }
    public OccurrenceStatus Status { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class ReminderRule
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PaymentPlanId { get; set; }
    public int DaysBefore { get; set; }
    public TimeOnly TimeOfDay { get; set; }
    public bool IsEnabled { get; set; } = true;
}
public sealed class UserSettings
{
    public Guid UserId { get; set; }
    public string LanguageCode { get; set; } = "tr";
    public string DefaultCurrencyCode { get; set; } = "TRY";
    public string TimeZoneId { get; set; } = "Europe/Istanbul";
    public bool NotificationsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed class DeviceToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Platform { get; set; } = "";
    public string PushToken { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime LastSeenAtUtc { get; set; }
}
public sealed class UserSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ExternalSubscriptionId { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime? LastVerifiedAtUtc { get; set; }
}
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
}
public sealed class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PaymentPlanId { get; set; }
    public DateOnly DueDate { get; set; }
    public Guid ReminderRuleId { get; set; }
    public DateTime ScheduledAtUtc { get; set; }
    public DeliveryStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? Metadata { get; set; }
}
