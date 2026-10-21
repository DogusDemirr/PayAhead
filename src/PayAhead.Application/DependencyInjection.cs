using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayAhead.Application.Payments;

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
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
