using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Application.Auth;

namespace PayAhead.Api;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = "PayAhead";
    public string Audience { get; init; } = "PayAhead.Mobile";
    public string? SigningKey { get; init; }
    public int AccessLifetimeMinutes { get; init; } = 15;
    public int RefreshLifetimeDays { get; init; } = 30;
    public byte[] KeyBytes { get; private set; } = [];

    public static JwtSettings Load(IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = configuration.GetSection("Jwt").Get<JwtSettings>() ?? new();
        if (string.IsNullOrWhiteSpace(settings.Issuer) || string.IsNullOrWhiteSpace(settings.Audience)
            || settings.AccessLifetimeMinutes is < 1 or > 30 || settings.RefreshLifetimeDays is < 1 or > 90)
            throw new InvalidOperationException("JWT issuer/audience veya token süresi geçersiz.");
        if (string.IsNullOrEmpty(settings.SigningKey) && environment.IsDevelopment())
            settings.KeyBytes = RandomNumberGenerator.GetBytes(64);
        else
        {
            try { settings.KeyBytes = Convert.FromBase64String(settings.SigningKey ?? ""); }
            catch (FormatException) { throw new InvalidOperationException("JWT signing key Base64 olmalıdır."); }
            if (settings.KeyBytes.Length < 32 || settings.KeyBytes.Distinct().Count() < 16)
                throw new InvalidOperationException("JWT signing key en az 32 rastgele byte içermelidir.");
        }
        return settings;
    }

    public TokenValidationParameters ValidationParameters() => new()
    {
        RequireSignedTokens = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(KeyBytes),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(15),
        NameClaimType = JwtRegisteredClaimNames.Sub
    };
}

public sealed class TokenService(JwtSettings settings, TimeProvider clock) : ITokenService
{
    public int RefreshLifetimeDays => settings.RefreshLifetimeDays;
    public AccessTokenResult CreateAccessToken(Guid userId, Guid securityStamp = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.AccessLifetimeMinutes);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience,
            [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
             new Claim("security_stamp", securityStamp.ToString("N")),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
             new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)],
            now, expires, new SigningCredentials(new SymmetricSecurityKey(settings.KeyBytes), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
    public string CreateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
