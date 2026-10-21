using System.Text.Json.Serialization;
using PayAhead.Domain;

namespace PayAhead.Application.Premium;

public enum LimitResource { PaymentPlans, Reminders, TrackedSubscriptions }
public sealed record PlanLimits(int ActivePaymentPlans, int ActiveRemindersPerPlan, int ActiveTrackedSubscriptions);
public sealed class MembershipOptions
{
    public PlanLimits Free { get; } = new(5, 1, 3);
    public int PremiumPaymentPlans { get; init; } = 1000;
    public int PremiumReminders { get; init; } = 10;
    public int PremiumTrackedSubscriptions { get; init; } = 1000;
    public int VerificationFreshnessHours { get; init; } = 24;
    public PlanLimits Premium => new(PremiumPaymentPlans, PremiumReminders, PremiumTrackedSubscriptions);
}
public sealed record EntitlementResponse(string Plan, MembershipState State, bool IsPremium, PlanLimits Limits,
    DateTime? AccessEndsAtUtc, bool AutoRenew, Guid AppAccountToken, string GoogleObfuscatedAccountId,
    bool BasicDashboard = true, bool Calendar = true, bool AdvancedSubscriptionTracking = false);
public interface IEntitlementService
{
    Task<EntitlementResponse> GetAsync(Guid user, CancellationToken ct);
    Task LockUserAsync(Guid user, CancellationToken ct);
    Task EnsureIncreaseAsync(Guid user, LimitResource resource, Guid? plan, int increase, CancellationToken ct);
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PurchaseRequest(string? Store, string? ProductId, string? Evidence);
public sealed record VerifiedPurchase(string Store, string ProductId, string OriginalId, string TransactionId,
    string? TokenHash, string? LinkedTokenHash, Guid AccountUserId, MembershipState State, DateTime ExpiresAtUtc,
    bool AutoRenew, string Environment, string ReconcileEvidence);
public sealed record VerifiedWebhook(string Store, string EventId, string Reference);
public interface IStorePurchaseVerifier
{
    Task<VerifiedPurchase> VerifyAsync(Guid user, PurchaseRequest request, CancellationToken ct);
    Task<VerifiedPurchase> ReconcileAsync(UserSubscription subscription, string evidence, CancellationToken ct);
}
public interface IStoreWebhookAuthenticator
{
    Task<VerifiedWebhook> AuthenticateAsync(string store, string body, string? authorization, CancellationToken ct);
}
public interface IPremiumService
{
    Task<EntitlementResponse> VerifyAsync(Guid user, PurchaseRequest request, CancellationToken ct);
    Task ReceiveAsync(string store, string body, string? authorization, CancellationToken ct);
    Task ProcessAsync(CancellationToken ct);
}
public sealed class EntitlementLimitException() : Exception;
public sealed class StoreVerificationException() : Exception;
public sealed class StoreUnavailableException() : Exception;
