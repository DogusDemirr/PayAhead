using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayAhead.Application.Payments;
using PayAhead.Application.Occurrences;
using PayAhead.Application.Overview;

namespace PayAhead.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("PayAhead");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("ConnectionStrings:PayAhead yapılandırılmalıdır.");
        services.AddDbContext<PayAheadDbContext>((provider, options) =>
            options.UseSqlServer(provider.GetRequiredService<IConfiguration>().GetConnectionString("PayAhead"),
                sql => sql.EnableRetryOnFailure(3).CommandTimeout(30)));
        services.AddScoped<IPaymentPlanStore, PaymentPlanStore>();
        services.AddScoped<IOccurrenceStore, OccurrenceStore>();
        services.AddScoped<IOverviewStore, OverviewStore>();
        services.AddScoped<PayAhead.Application.Auth.IAuthStore, AuthStore>();
        services.AddScoped<PayAhead.Application.Auth.IPasswordResetStore, PasswordResetStore>();
        services.AddScoped<PayAhead.Application.Auth.IPasswordResetEmail, SmtpPasswordResetEmail>();
        services.AddSingleton<PasswordResetEmailWorker>();
        services.AddSingleton<PayAhead.Application.Auth.IPasswordResetDispatcher>(provider => provider.GetRequiredService<PasswordResetEmailWorker>());
        services.AddHostedService(provider => provider.GetRequiredService<PasswordResetEmailWorker>());
        var reset = configuration.GetSection("PasswordReset").Get<PayAhead.Application.Auth.PasswordResetOptions>() ?? new();
        if (reset.LifetimeMinutes is < 5 or > 60) throw new InvalidOperationException("PasswordReset lifetime must be 5–60 minutes.");
        if (configuration.GetValue<bool>("Email:Enabled") && (!Uri.TryCreate(reset.LinkBase, UriKind.Absolute, out var resetUri)
            || resetUri.Scheme is not ("https" or "exp" or "payahead") || resetUri.Query.Length != 0 || resetUri.Fragment.Length != 0))
            throw new InvalidOperationException("PasswordReset:LinkBase must be a trusted absolute reset-password link without query/fragment.");
        services.AddSingleton(reset);
        services.AddSingleton<PayAhead.Application.Auth.IPasswordService, IdentityPasswordService>();
        services.AddScoped<PayAhead.Application.Notifications.INotificationManagementStore, NotificationManagementStore>();
        services.AddScoped<PayAhead.Application.Notifications.INotificationQueue, SqlNotificationQueue>();
        services.AddScoped<PayAhead.Application.Subscriptions.ISubscriptionStore, SubscriptionStore>();
        services.AddScoped<PayAhead.Application.Operations.ISharedRateLimiter, SqlSharedRateLimiter>();
        services.AddScoped<PayAhead.Application.Operations.IAccountStore, AccountStore>();
        var maintenance = configuration.GetSection("Maintenance").Get<PayAhead.Application.Operations.MaintenanceOptions>() ?? new();
        if (maintenance.RefreshRetentionDays is < 90 or > 3650 || maintenance.BatchSize is < 1 or > 1000) throw new InvalidOperationException("Maintenance retention/batch is invalid.");
        services.AddSingleton(maintenance);
        services.AddScoped<MaintenanceStore>();
        services.AddSingleton(NotificationOptions.Load(configuration));
        services.AddHttpClient("ExpoPush", client => client.Timeout = TimeSpan.FromSeconds(15))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton<PayAhead.Application.Notifications.IPushNotificationSender, ExpoPushNotificationSender>();
        if (configuration.GetValue<bool>("Stores:UseFake")) throw new InvalidOperationException("Fake store providers are test-assembly only.");
        var membership = configuration.GetSection("Membership").Get<PayAhead.Application.Premium.MembershipOptions>() ?? new();
        if (membership.PremiumPaymentPlans is < 5 or > 1000 || membership.PremiumReminders is < 1 or > 10 || membership.PremiumTrackedSubscriptions is < 3 or > 1000
            || membership.VerificationFreshnessHours is < 1 or > 24) throw new InvalidOperationException("Membership limits are invalid.");
        services.AddSingleton(membership); services.AddSingleton(StoreOptions.Load(configuration)); services.AddSingleton<PurchaseProtector>();
        services.AddScoped<PayAhead.Application.Premium.IEntitlementService, SqlEntitlementService>();
        services.AddScoped<PayAhead.Application.Premium.IPremiumService, PremiumService>();
        services.AddScoped<PayAhead.Application.Premium.IStorePurchaseVerifier, StorePurchaseVerifier>();
        services.AddScoped<PayAhead.Application.Premium.IStoreWebhookAuthenticator, StoreWebhookAuthenticator>();
        services.AddSingleton<AppleSignedDataVerifier>(); services.AddSingleton<StoreHttp>(); services.AddSingleton<GooglePushSignatureValidator>();
        services.AddScoped<AppleStoreClient>(); services.AddScoped<GoogleStoreClient>();
        services.AddHttpClient("Stores", client => client.Timeout = TimeSpan.FromSeconds(15)).RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        return services;
    }
}
