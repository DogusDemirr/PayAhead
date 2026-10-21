using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayAhead.Application.Payments;
using PayAhead.Domain.Recurrence;

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
        return services;
    }
}
