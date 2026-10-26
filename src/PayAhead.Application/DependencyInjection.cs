using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayAhead.Application.Payments;
using PayAhead.Domain.Recurrence;
using PayAhead.Application.Occurrences;
using PayAhead.Application.Overview;

namespace PayAhead.Application;

public interface ICurrentUser
{
    Guid? UserId { get; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ICurrentUser>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPaymentRecurrenceCalculator, PaymentRecurrenceCalculator>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<PayAhead.Application.Catalog.CatalogReader>();
        services.AddScoped<PayAhead.Application.Localization.LanguagePreferenceService>();
        services.AddScoped<IOccurrenceService, OccurrenceService>();
        services.AddScoped<IOverviewService, OverviewService>();
        services.AddScoped<IAnalyticsService, OverviewService>();
        services.AddScoped<PayAhead.Application.Auth.IAuthService, PayAhead.Application.Auth.AuthService>();
        services.AddScoped<PayAhead.Application.Auth.PasswordResetService>();
        services.AddScoped<PayAhead.Application.Notifications.INotificationManagementService, PayAhead.Application.Notifications.NotificationManagementService>();
        services.AddScoped<PayAhead.Application.Notifications.NotificationProcessor>();
        services.AddScoped<PayAhead.Application.Subscriptions.ISubscriptionService, PayAhead.Application.Subscriptions.SubscriptionService>();
        return services;
    }
}
