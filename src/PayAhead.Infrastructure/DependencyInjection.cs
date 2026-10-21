using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
