using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PayAhead.Application;

namespace PayAhead.Api;

public static class DevelopmentIdentity
{
    public const string Scheme = "DevelopmentIdentity";

    public static Guid? Validate(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("DevelopmentIdentity:Enabled")) return null;
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Geliştirme kimliği yalnızca Development ortamında kullanılabilir.");
        if (!Guid.TryParse(configuration["DevelopmentIdentity:UserId"], out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Geliştirme kimliği için geçerli bir UserId gereklidir.");
        return id;
    }
}

public sealed class DevelopmentIdentityHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Kimlik HTTP başlığından alınmaz; sadece sunucu yapılandırması kullanılır.
        var id = configuration["DevelopmentIdentity:UserId"]!;
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        && id != Guid.Empty ? id : null;
}
