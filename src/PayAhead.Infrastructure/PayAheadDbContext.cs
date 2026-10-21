using Microsoft.EntityFrameworkCore;
using PayAhead.Domain;

namespace PayAhead.Infrastructure;

public sealed class PayAheadDbContext(DbContextOptions<PayAheadDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<PaymentOccurrence> PaymentOccurrences => Set<PaymentOccurrence>();
    public DbSet<ReminderRule> ReminderRules => Set<ReminderRule>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder model)
        => model.ApplyConfigurationsFromAssembly(typeof(PayAheadDbContext).Assembly);
}
