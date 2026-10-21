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
                sql => sql.EnableRetryOnFailure(3)));
        services.AddScoped<IPaymentPlanStore, PaymentPlanStore>();
        services.AddScoped<IOccurrenceStore, OccurrenceStore>();
        services.AddScoped<IOverviewStore, OverviewStore>();
        services.AddScoped<PayAhead.Application.Auth.IAuthStore, AuthStore>();
        services.AddSingleton<PayAhead.Application.Auth.IPasswordService, IdentityPasswordService>();
        services.AddScoped<PayAhead.Application.Notifications.INotificationManagementStore, NotificationManagementStore>();
        services.AddScoped<PayAhead.Application.Notifications.INotificationQueue, SqlNotificationQueue>();
        services.AddSingleton(NotificationOptions.Load(configuration));
        services.AddHttpClient("ExpoPush", client => client.Timeout = TimeSpan.FromSeconds(15))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton<PayAhead.Application.Notifications.IPushNotificationSender, ExpoPushNotificationSender>();
        return services;
    }
}
