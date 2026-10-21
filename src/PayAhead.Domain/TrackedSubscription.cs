namespace PayAhead.Domain;

public enum SubscriptionStatus { Trial, Active, Cancelled, Expired }
public enum ReminderTarget { Payment, SubscriptionTrial }

// External subscriptions tracked by the user; UserSubscription remains reserved for store entitlements.
public sealed class TrackedSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PaymentPlanId { get; set; }
    public string ProviderName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? TrialStartDate { get; set; }
    public DateOnly? TrialEndDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public bool AutoRenew { get; set; }
    public bool EnableTrialReminders { get; set; } = true;
    public DateTime? CancellationRequestedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateOnly? AccessEndsOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public SubscriptionStatus EffectiveStatus(DateOnly today)
    {
        if (AccessEndsOn is { } end && today > end) return SubscriptionStatus.Expired;
        if (Status == SubscriptionStatus.Cancelled) return SubscriptionStatus.Cancelled;
        if (Status == SubscriptionStatus.Trial && TrialEndDate is { } trial && today > trial)
            return AutoRenew ? SubscriptionStatus.Active : SubscriptionStatus.Expired;
        return Status;
    }
    public int? TrialDaysRemaining(DateOnly today) => TrialEndDate is { } end ? Math.Max(0, end.DayNumber - today.DayNumber) : null;
    public bool TrialReminderEligible(DateOnly today, DateOnly due)
        => !IsDeleted && EnableTrialReminders && Status == SubscriptionStatus.Trial && EffectiveStatus(today) == SubscriptionStatus.Trial
            && TrialStartDate <= today && TrialEndDate == due;
    public void EnsureMutable(DateOnly today)
    {
        if (IsDeleted || EffectiveStatus(today) is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            throw new DomainRuleException("İptal edilmiş veya süresi bitmiş abonelik değiştirilemez.");
    }
    public void Transition(SubscriptionStatus status, DateOnly today)
    {
        EnsureMutable(today);
        if (status is not (SubscriptionStatus.Trial or SubscriptionStatus.Active)
            || EffectiveStatus(today) == SubscriptionStatus.Active && status == SubscriptionStatus.Trial)
            throw new DomainRuleException("Abonelik durum geçişi geçersiz.");
        Status = status;
    }
    public void Cancel(DateOnly accessEnd, DateOnly today, DateTime now)
    {
        EnsureMutable(today);
        if (now.Kind != DateTimeKind.Utc || accessEnd < today || accessEnd < StartDate)
            throw new DomainRuleException("İptal erişim bitişi bugün veya sonrası olmalıdır.");
        Status = SubscriptionStatus.Cancelled; AutoRenew = false; AccessEndsOn = accessEnd;
        CancellationRequestedAtUtc = now; CancelledAtUtc = now; UpdatedAtUtc = now;
    }
}
