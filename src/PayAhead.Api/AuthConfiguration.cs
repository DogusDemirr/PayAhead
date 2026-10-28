using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Auth;
using PayAhead.Infrastructure;

namespace PayAhead.Api;

public static class AuthConfiguration
{
    public static IServiceCollection AddPayAheadAuthentication(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = JwtSettings.Load(configuration, environment);
        services.AddSingleton(settings);
        services.AddSingleton<ITokenService, TokenService>();
        services.AddHttpsRedirection(options => options.HttpsPort = configuration.GetValue<int?>("HttpsPort") ?? 443);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = false;
            options.SaveToken = false;
            options.TokenValidationParameters = settings.ValidationParameters();
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var stampClaim = context.Principal?.FindFirst("security_stamp")?.Value;
                    var stamp = Guid.Empty;
                    if (stampClaim is not null && !Guid.TryParse(stampClaim, out stamp)) { context.Fail("Invalid identity."); return; }
                    var subject = context.Principal?.FindAll(JwtRegisteredClaimNames.Sub).ToArray() ?? [];
                    if (subject.Length != 1 || !Guid.TryParse(subject[0].Value, out var id) || id == Guid.Empty
                        || !await context.HttpContext.RequestServices.GetRequiredService<PayAheadDbContext>().Users
                            .AsNoTracking().AnyAsync(x => x.Id == id && x.IsActive && x.SecurityStamp == stamp, context.HttpContext.RequestAborted))
                        context.Fail("Invalid identity.");
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.Headers.WWWAuthenticate = "Bearer";
                    await Results.Problem(statusCode: 401, title: "Kimlik doğrulama gerekli.").ExecuteAsync(context.HttpContext);
                },
                OnForbidden = context => Results.Problem(statusCode: 403, title: "Erişim reddedildi.").ExecuteAsync(context.HttpContext)
            };
        });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Results.Problem(statusCode: 429, title: "Çok fazla kimlik doğrulama isteği.").ExecuteAsync(context.HttpContext);
            };
        });
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // No forwarded headers are processed unless an explicit proxy allowlist is present.
            var proxies = configuration.GetSection("TrustedProxies").Get<string[]>() ?? [];
            options.KnownNetworks.Clear(); options.KnownProxies.Clear();
            if (proxies.Length == 0) { options.ForwardedHeaders = ForwardedHeaders.None; return; }
            foreach (var proxy in proxies) options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            options.ForwardLimit = 1;
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        });
        return services;
    }
}
