using Microsoft.EntityFrameworkCore;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class PayAheadDbContext(DbContextOptions<PayAheadDbContext> options) : DbContext(options)
{
    public DbSet<CatalogCategory> CatalogCategories => Set<CatalogCategory>();
    public DbSet<CatalogCategoryTranslation> CatalogCategoryTranslations => Set<CatalogCategoryTranslation>();
    public DbSet<CatalogService> CatalogServices => Set<CatalogService>();
    public DbSet<CatalogServiceCategory> CatalogServiceCategories => Set<CatalogServiceCategory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<PaymentOccurrence> PaymentOccurrences => Set<PaymentOccurrence>();
    public DbSet<ReminderRule> ReminderRules => Set<ReminderRule>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<StorePurchase> StorePurchases => Set<StorePurchase>();
    public DbSet<StoreTransaction> StoreTransactions => Set<StoreTransaction>();
    public DbSet<StoreWebhookInbox> StoreWebhookInbox => Set<StoreWebhookInbox>();
    public DbSet<TrackedSubscription> TrackedSubscriptions => Set<TrackedSubscription>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RateLimitBucket> RateLimitBuckets => Set<RateLimitBucket>();

    protected override void OnModelCreating(ModelBuilder model)
        => model.ApplyConfigurationsFromAssembly(typeof(PayAheadDbContext).Assembly);
}
