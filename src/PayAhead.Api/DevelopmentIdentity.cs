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

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => Guid.TryParse((accessor.HttpContext?.User.Identity?.IsAuthenticated == true ? accessor.HttpContext.User.FindFirstValue("sub") : null), out var id)
        && id != Guid.Empty ? id : null;
}
