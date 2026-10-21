using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace PayAhead.IntegrationTests;

// This handler lives only in the test assembly. The shipping API never registers it.
internal static class TestIdentity
{
    public static IWebHostBuilder UseTestIdentity(this IWebHostBuilder builder) => builder.ConfigureTestServices(services =>
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "TestIdentity";
            options.DefaultAuthenticateScheme = "TestIdentity";
            options.DefaultChallengeScheme = "TestIdentity";
        }).AddScheme<AuthenticationSchemeOptions, TestIdentityHandler>("TestIdentity", _ => { }));
}
internal sealed class TestIdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!config.GetValue<bool>("DevelopmentIdentity:Enabled")) return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", config["DevelopmentIdentity:UserId"]!)], Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        => Results.Problem(statusCode: 401).ExecuteAsync(Context);
}
