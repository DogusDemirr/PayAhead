namespace PayAhead.Domain;

public enum MembershipState { Free, Active, GracePeriod, BillingRetry, Expired, Revoked }
public static class MembershipRules
{
    public static bool GrantsPremium(UserSubscription value, DateTime now, TimeSpan maximumAge)
        => value.IsStoreVerified && value.LastVerifiedAtUtc is { } verified && verified <= now.AddMinutes(1)
            && verified >= now.Subtract(maximumAge) && value.CurrentPeriodEndUtc > now
            && (value.Status == nameof(MembershipState.Active) || value.Status == nameof(MembershipState.GracePeriod));
}
public sealed class StorePurchase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid UserSubscriptionId { get; set; }
    public string Store { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public string? TokenHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
public sealed class StoreWebhookInbox
{
    public Guid Id { get; set; }
    public string Store { get; set; } = "";
    public string EventId { get; set; } = "";
    public string ReferenceHash { get; set; } = "";
    public string ProtectedReference { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorCode { get; set; }
}
public sealed class StoreTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid UserSubscriptionId { get; set; }
    public string Store { get; set; } = "";
    public string TransactionId { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
