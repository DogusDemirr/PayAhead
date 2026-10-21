using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PayAhead.Application;

public interface ICurrentUser
{
    Guid? UserId { get; }
}

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
        => services.AddValidatorsFromAssemblyContaining<ICurrentUser>();
}
