using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Api;
using PayAhead.Application;
using PayAhead.Application.Auth;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;

namespace PayAhead.UnitTests;

public sealed class AuthTests
{
    public static JwtSettings Settings(string environment = "Development", string? key = null)
        => JwtSettings.Load(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key }).Build(), new EnvironmentStub(environment));
    private sealed class EnvironmentStub(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    [Theory]
    [InlineData(null, "valid-long-password", "Name")]
    [InlineData("", "valid-long-password", "Name")]
    [InlineData("invalid", "valid-long-password", "Name")]
    [InlineData("a@b.com", "", "Name")]
    [InlineData("a@b.com", "short", "Name")]
    [InlineData("a@b.com", "valid-long-password", " ")]
    public void Registration_rejects_invalid_fields(string? email, string password, string name)
        => Assert.False(new RegisterValidator().Validate(new RegisterRequest(email, password, name)).IsValid);
    [Fact]
    public void Registration_accepts_long_passphrases_and_trimmed_email()
        => Assert.True(new RegisterValidator().Validate(new RegisterRequest(" User@example.com ", new string('p', 128), "Name")).IsValid);
    [Fact]
    public void Registration_caps_password_length()
        => Assert.False(new RegisterValidator().Validate(new RegisterRequest("a@b.com", new string('p', 129), "Name")).IsValid);
    [Fact] public void Email_normalization_is_invariant() => Assert.Equal("USER@EXAMPLE.COM", EmailNormalization.Normalize(" user@example.com "));
    [Fact]
    public void Identity_hash_is_salted_verifiable_and_never_plaintext()
    {
        var hasher = new IdentityPasswordService(); var user = new User();
        user.PasswordHash = hasher.Hash(user, "a-long-password");
        Assert.NotEqual(user.PasswordHash, hasher.Hash(user, "a-long-password"));
        Assert.DoesNotContain("a-long-password", user.PasswordHash);
        Assert.True(hasher.Verify(user, "a-long-password", out var rehash)); Assert.False(rehash);
        Assert.False(hasher.Verify(user, "wrong", out _));
        user.PasswordHash = "legacy-placeholder"; Assert.False(hasher.Verify(user, "wrong", out _));
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("weak")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void Production_rejects_missing_or_weak_key(string? key) => Assert.Throws<InvalidOperationException>(() => Settings("Production", key));
    [Fact]
    public void Jwt_contains_only_identifiers_and_has_short_lifetime()
    {
        var settings = Settings(); var tokens = new TokenService(settings, TimeProvider.System); var id = Guid.NewGuid();
        var result = tokens.CreateAccessToken(id); var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(id.ToString(), jwt.Subject); Assert.Equal("HS256", jwt.Header.Alg);
        Assert.Equal(TimeSpan.FromMinutes(15), jwt.ValidTo - jwt.ValidFrom);
        Assert.Contains(jwt.Claims, x => x.Type == "iat"); Assert.Contains(jwt.Claims, x => x.Type == "jti");
        Assert.DoesNotContain(jwt.Claims, x => x.Type is "email" or "name" or "password");
        _ = new JwtSecurityTokenHandler().ValidateToken(result.Token, settings.ValidationParameters(), out _);
        Assert.Equal(TimeSpan.FromSeconds(15), settings.ValidationParameters().ClockSkew);
    }
    [Fact]
    public void Refresh_tokens_have_384_bits_of_randomness_and_only_hash_is_persistable()
    {
        var tokens = new TokenService(Settings(), TimeProvider.System);
        var generated = Enumerable.Range(0, 100).Select(_ => tokens.CreateRefreshToken()).ToArray();
        Assert.Equal(100, generated.Distinct().Count());
        Assert.All(generated, x => { Assert.Equal(48, Base64UrlEncoder.DecodeBytes(x).Length); Assert.True(new RefreshValidator().Validate(new RefreshRequest(x)).IsValid); });
        Assert.Equal(64, tokens.HashRefreshToken(generated[0]).Length);
        Assert.Equal(tokens.HashRefreshToken(generated[0]), tokens.HashRefreshToken(generated[0]));
        Assert.NotEqual(tokens.HashRefreshToken(generated[0]), tokens.HashRefreshToken(generated[1]));
    }
    [Theory]
    [InlineData(false, "sub")]
    [InlineData(true, "userId")]
    [InlineData(true, ClaimTypes.NameIdentifier)]
    public void Current_user_rejects_unverified_or_unexpected_claim(bool authenticated, string type)
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(type, Guid.NewGuid().ToString())], authenticated ? "Bearer" : null)) };
        Assert.Null(new CurrentUser(new HttpContextAccessor { HttpContext = context }).UserId);
    }
    [Fact]
    public void Current_user_accepts_verified_subject()
    {
        var id = Guid.NewGuid(); var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", id.ToString())], "Bearer")) };
        Assert.Equal(id, new CurrentUser(new HttpContextAccessor { HttpContext = context }).UserId);
    }

    private sealed class MemoryStore : IAuthStore
    {
        public User? User; public RefreshToken? Session; public bool Reused;
        public Task RegisterAsync(User user, CancellationToken ct, string? legalVersion = null)
        { if (User is not null) throw new ResourceConflictException("Duplicate"); User = user; return Task.CompletedTask; }
        public Task<User?> AuthenticateAsync(string email, Func<User, bool> verify, DateTime now, CancellationToken ct)
            => Task.FromResult(User is { IsActive: true } && User.NormalizedEmail == email && verify(User) ? User : null);
        public Task AddSessionAsync(RefreshToken token, CancellationToken ct, Guid securityStamp = default) { Session = token; return Task.CompletedTask; }
        public Task<User?> RotateAsync(string hash, RefreshToken replacement, DateTime now, CancellationToken ct)
        {
            if (Session is null || Session.TokenHash != hash || Session.RevokedAtUtc is not null) { Reused = true; if (Session is not null) Session.RevokedAtUtc = now; return Task.FromResult<User?>(null); }
            replacement.UserId = Session.UserId; replacement.FamilyId = Session.FamilyId; Session = replacement; return Task.FromResult(User);
        }
        public Task RevokeAsync(string hash, DateTime now, CancellationToken ct) { if (Session is not null) Session.RevokedAtUtc = now; return Task.CompletedTask; }
        public Task<User?> FindActiveAsync(Guid id, CancellationToken ct) => Task.FromResult(User?.Id == id ? User : null);
    }
    private sealed class NoUser : ICurrentUser { public Guid? UserId => null; }
    private static AuthService Service(MemoryStore store) => new(store, new IdentityPasswordService(), new TokenService(Settings(), TimeProvider.System), new NoUser(), TimeProvider.System, new RegisterValidator(), new LoginValidator(), new RefreshValidator());
    [Fact]
    public async Task Registration_login_rotation_reuse_and_logout_service_contract()
    {
        var store = new MemoryStore(); var service = Service(store); var ct = CancellationToken.None;
        var user = await service.RegisterAsync(new(" first@example.com ", "correct-long-password", " Name "), ct);
        Assert.Equal("Name", user.DisplayName); Assert.Equal("FIRST@EXAMPLE.COM", store.User!.NormalizedEmail);
        await Assert.ThrowsAsync<ResourceConflictException>(() => service.RegisterAsync(new("first@example.com", "correct-long-password", "Name"), ct));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => service.LoginAsync(new("first@example.com", "wrong"), ct));
        var login = await service.LoginAsync(new("FIRST@example.com", "correct-long-password"), ct);
        Assert.NotEqual(login.RefreshToken, store.Session!.TokenHash);
        var family = store.Session.FamilyId;
        var rotated = await service.RefreshAsync(new(login.RefreshToken), ct); Assert.Equal(family, store.Session.FamilyId);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => service.RefreshAsync(new(login.RefreshToken), ct)); Assert.True(store.Reused);
        await service.LogoutAsync(new(rotated.RefreshToken), ct); Assert.NotNull(store.Session.RevokedAtUtc);
        store.User.IsActive = false;
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => service.LoginAsync(new("first@example.com", "correct-long-password"), ct));
        await Assert.ThrowsAsync<AuthenticationRequiredException>(() => service.MeAsync(ct));
    }
}
