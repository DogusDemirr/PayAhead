using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users"); b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.NormalizedEmail).HasMaxLength(254).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.HasIndex(x => x.NormalizedEmail).IsUnique();
    }
}
public sealed class PaymentPlanConfiguration : IEntityTypeConfiguration<PaymentPlan>
{
    public void Configure(EntityTypeBuilder<PaymentPlan> b)
    {
        b.ToTable("PaymentPlans", t =>
        {
            t.HasCheckConstraint("CK_PaymentPlans_Amount", "[Amount] > 0");
            t.HasCheckConstraint("CK_PaymentPlans_Interval", "[Interval] > 0");
            t.HasCheckConstraint("CK_PaymentPlans_Frequency", "[Frequency] BETWEEN 0 AND 4");
            t.HasCheckConstraint("CK_PaymentPlans_Dates", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_PaymentPlans_Anchor", "[AnchorDay] IS NULL OR [AnchorDay] BETWEEN 1 AND 31");
            t.HasCheckConstraint("CK_PaymentPlans_Currency", "[CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%' AND LEN([CurrencyCode]) = 3");
        });
        b.HasKey(x => x.Id);
        b.HasAlternateKey(x => new { x.Id, x.UserId });
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Category).HasMaxLength(50).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        b.Property(x => x.Amount).HasPrecision(19, 4);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.UserId, x.IsActive, x.StartDate, x.Id });
    }
}
public sealed class PaymentOccurrenceConfiguration : IEntityTypeConfiguration<PaymentOccurrence>
{
    public void Configure(EntityTypeBuilder<PaymentOccurrence> b)
    {
        b.ToTable("PaymentOccurrences", t =>
        {
            t.HasCheckConstraint("CK_PaymentOccurrences_Status", "[Status] BETWEEN 0 AND 2");
            t.HasCheckConstraint("CK_PaymentOccurrences_Amount", "[ActualAmount] IS NULL OR [ActualAmount] > 0");
            t.HasCheckConstraint("CK_PaymentOccurrences_State", "([Status] = 1 AND [PaidAtUtc] IS NOT NULL AND [ActualAmount] IS NOT NULL) OR ([Status] <> 1 AND [PaidAtUtc] IS NULL AND [ActualAmount] IS NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.ActualAmount).HasPrecision(19, 4);
        b.Property(x => x.RowVersion).IsRowVersion();
        b.HasIndex(x => new { x.PaymentPlanId, x.DueDate }).IsUnique();
        b.HasOne<PaymentPlan>().WithMany().HasForeignKey(x => x.PaymentPlanId).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class ReminderRuleConfiguration : IEntityTypeConfiguration<ReminderRule>
{
    public void Configure(EntityTypeBuilder<ReminderRule> b)
    {
        b.ToTable("ReminderRules", t => t.HasCheckConstraint("CK_ReminderRules_DaysBefore", "[DaysBefore] BETWEEN 0 AND 365"));
        b.HasKey(x => x.Id);
        b.HasAlternateKey(x => new { x.Id, x.PaymentPlanId, x.UserId });
        b.HasOne<PaymentPlan>().WithMany().HasForeignKey(x => new { x.PaymentPlanId, x.UserId })
            .HasPrincipalKey(x => new { x.Id, x.UserId }).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.PaymentPlanId, x.Target, x.DaysBefore, x.TimeOfDay }).IsUnique().HasFilter("[IsDeleted] = 0");
        b.HasIndex(x => x.UserId);
    }
}
public sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> b)
    {
        b.ToTable("UserSettings"); b.HasKey(x => x.UserId);
        b.Property(x => x.LanguageCode).HasMaxLength(16).IsRequired();
        b.Property(x => x.DefaultCurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        b.HasOne<User>().WithOne().HasForeignKey<UserSettings>(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> b)
    {
        b.ToTable("DeviceTokens", t => t.HasCheckConstraint("CK_DeviceTokens_Platform", "[Platform] IN ('ios', 'android')"));
        b.HasKey(x => x.Id);
        b.HasAlternateKey(x => new { x.Id, x.UserId });
        b.Property(x => x.Platform).HasMaxLength(16).IsUnicode(false).IsRequired();
        b.Property(x => x.PushToken).HasMaxLength(512).IsUnicode(false).UseCollation("Latin1_General_100_BIN2").IsRequired();
        b.HasIndex(x => x.PushToken).IsUnique();
        b.HasIndex(x => new { x.UserId, x.IsActive });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> b)
    {
        b.ToTable("UserSubscriptions"); b.HasKey(x => x.Id);
        b.HasAlternateKey(x=>new{x.Id,x.UserId});
        b.Property(x=>x.RowVersion).IsRowVersion();b.Property(x=>x.StoreEnvironment).HasMaxLength(16).IsUnicode(false);
        b.Property(x=>x.ProtectedEvidence).HasColumnType("varchar(max)");
        b.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        b.Property(x => x.ProductId).HasMaxLength(200).IsRequired();
        b.Property(x => x.ExternalSubscriptionId).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasMaxLength(32).IsRequired();
        b.HasIndex(x => new { x.Provider, x.ExternalSubscriptionId }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.CurrentPeriodEndUtc });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class StorePurchaseConfiguration : IEntityTypeConfiguration<StorePurchase>
{
    public void Configure(EntityTypeBuilder<StorePurchase> b)
    {
        b.HasKey(x=>x.Id);b.Property(x=>x.Store).HasMaxLength(16).IsUnicode(false);b.Property(x=>x.TransactionId).HasMaxLength(200).IsUnicode(false).UseCollation("Latin1_General_100_BIN2");
        b.Property(x=>x.TokenHash).HasMaxLength(64).IsUnicode(false).UseCollation("Latin1_General_100_BIN2");
        b.HasIndex(x=>new{x.Store,x.TransactionId}).IsUnique();b.HasIndex(x=>new{x.Store,x.TokenHash}).IsUnique().HasFilter("[TokenHash] IS NOT NULL");
        b.HasOne<UserSubscription>().WithMany().HasForeignKey(x=>new{x.UserSubscriptionId,x.UserId}).HasPrincipalKey(x=>new{x.Id,x.UserId}).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class StoreWebhookInboxConfiguration : IEntityTypeConfiguration<StoreWebhookInbox>
{
    public void Configure(EntityTypeBuilder<StoreWebhookInbox> b)
    {
        b.ToTable("StoreWebhookInbox");b.HasKey(x=>x.Id);b.Property(x=>x.Store).HasMaxLength(16).IsUnicode(false);
        b.Property(x=>x.EventId).HasMaxLength(200).IsUnicode(false).UseCollation("Latin1_General_100_BIN2");b.Property(x=>x.ReferenceHash).HasMaxLength(64).IsUnicode(false);
        b.Property(x=>x.ProtectedReference).HasColumnType("varchar(max)");b.Property(x=>x.ErrorCode).HasMaxLength(64).IsUnicode(false);
        b.HasIndex(x=>new{x.Store,x.EventId}).IsUnique();b.HasIndex(x=>new{x.CompletedAtUtc,x.NextAttemptAtUtc,x.LeaseExpiresAtUtc});
    }
}
public sealed class StoreTransactionConfiguration : IEntityTypeConfiguration<StoreTransaction>
{
    public void Configure(EntityTypeBuilder<StoreTransaction> b)
    {
        b.HasKey(x=>x.Id);b.Property(x=>x.Store).HasMaxLength(16).IsUnicode(false);
        b.Property(x=>x.TransactionId).HasMaxLength(200).IsUnicode(false).UseCollation("Latin1_General_100_BIN2");
        b.HasIndex(x=>new{x.Store,x.TransactionId}).IsUnique();
        b.HasOne<UserSubscription>().WithMany().HasForeignKey(x=>new{x.UserSubscriptionId,x.UserId}).HasPrincipalKey(x=>new{x.Id,x.UserId}).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class TrackedSubscriptionConfiguration : IEntityTypeConfiguration<TrackedSubscription>
{
    public void Configure(EntityTypeBuilder<TrackedSubscription> b)
    {
        b.ToTable("TrackedSubscriptions",t=>
        {
            t.HasCheckConstraint("CK_TrackedSubscriptions_Status","[Status] BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_TrackedSubscriptions_Trial","([TrialStartDate] IS NULL AND [TrialEndDate] IS NULL) OR ([TrialStartDate] IS NOT NULL AND [TrialEndDate] IS NOT NULL AND [TrialEndDate] >= [TrialStartDate] AND [TrialStartDate] >= [StartDate])");
            t.HasCheckConstraint("CK_TrackedSubscriptions_TrialRequired","[Status] <> 0 OR [TrialEndDate] IS NOT NULL");
            t.HasCheckConstraint("CK_TrackedSubscriptions_Access","[AccessEndsOn] IS NULL OR [AccessEndsOn] >= [StartDate]");
        });
        b.HasKey(x=>x.Id);b.Property(x=>x.ProviderName).HasMaxLength(100).IsRequired();b.Property(x=>x.RowVersion).IsRowVersion();
        b.HasIndex(x=>x.PaymentPlanId).IsUnique();b.HasIndex(x=>new{x.UserId,x.IsDeleted,x.CreatedAtUtc,x.Id});
        b.HasOne<PaymentPlan>().WithOne().HasForeignKey<TrackedSubscription>(x=>new{x.PaymentPlanId,x.UserId})
            .HasPrincipalKey<PaymentPlan>(x=>new{x.Id,x.UserId}).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens", t => t.HasCheckConstraint("CK_RefreshTokens_Expiry", "[ExpiresAtUtc] > [CreatedAtUtc]"));
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(64).IsUnicode(false).UseCollation("Latin1_General_100_BIN2").IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
        b.HasIndex(x => new { x.UserId, x.FamilyId });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.NoAction);
    }
}
public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> b)
    {
        b.ToTable("NotificationDeliveries", t =>
        {
            t.HasCheckConstraint("CK_NotificationDeliveries_Attempts", "[AttemptCount] >= 0");
            t.HasCheckConstraint("CK_NotificationDeliveries_Status", "[Status] BETWEEN 0 AND 5");
            t.HasCheckConstraint("CK_NotificationDeliveries_ReceiptAttempts", "[ReceiptAttemptCount] >= 0");
        });
        b.HasKey(x => x.Id);
        b.HasOne<ReminderRule>().WithMany().HasForeignKey(x => new { x.ReminderRuleId, x.PaymentPlanId, x.UserId })
            .HasPrincipalKey(x => new { x.Id, x.PaymentPlanId, x.UserId }).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<DeviceToken>().WithMany().HasForeignKey(x => new { x.DeviceTokenId, x.UserId })
            .HasPrincipalKey(x => new { x.Id, x.UserId }).OnDelete(DeleteBehavior.NoAction);
        b.Property(x => x.ProviderMessageId).HasMaxLength(128).IsUnicode(false);
        b.Property(x => x.LastErrorCode).HasMaxLength(64).IsUnicode(false);
        b.HasIndex(x => new { x.ReminderRuleId, x.DueDate, x.DeviceTokenId }).IsUnique().HasFilter("[DeviceTokenId] IS NOT NULL");
        b.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.LeaseExpiresAtUtc });
        b.HasIndex(x => new { x.Status, x.ScheduledAtUtc });
        b.HasIndex(x => new { x.UserId, x.DueDate });
    }
}
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs"); b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Metadata).HasMaxLength(4000);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc, x.Id });
    }
}
