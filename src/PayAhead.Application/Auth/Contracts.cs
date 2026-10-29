using System.Text.Json.Serialization;
using PayAhead.Domain;

namespace PayAhead.Application.Auth;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegisterRequest(string? Email, string? Password, string? DisplayName, string? LegalVersion = null, RegistrationLegal? Legal = null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegistrationLegal(string? NoticeVersion, bool NoticeAcknowledged, string? PrivacyVersion, bool PrivacyAcknowledged, string? TermsVersion, bool TermsAccepted);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LoginRequest(string? Email, string? Password);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RefreshRequest(string? RefreshToken);
public sealed record UserResponse(Guid Id, string Email, string DisplayName)
{
    public bool HasAvatar { get; init; }
    public string AvatarVersion { get; init; } = "";
    public static UserResponse From(User user) => new(user.Id, user.Email, user.DisplayName) { HasAvatar = user.AvatarKey is not null, AvatarVersion = Convert.ToBase64String(user.RowVersion) };
}
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc, UserResponse User, string TokenType = "Bearer");
public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
public interface IPasswordService
{
    string Hash(User user, string password);
    bool Verify(User user, string password, out bool needsRehash);
    bool VerifyStored(User user, string hash, string password);
}
public interface ITokenService
{
    AccessTokenResult CreateAccessToken(Guid userId, Guid securityStamp = default);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
    int RefreshLifetimeDays { get; }
}
public interface IAuthStore
{
    Task RegisterAsync(User user, CancellationToken ct, string? legalVersion = null, RegistrationLegal? legal = null);
    Task<User?> AuthenticateAsync(string email, Func<User, bool> verify, DateTime now, CancellationToken ct);
    Task AddSessionAsync(RefreshToken token, CancellationToken ct, Guid securityStamp = default);
    Task<User?> RotateAsync(string hash, RefreshToken replacement, DateTime now, CancellationToken ct);
    Task RevokeAsync(string hash, DateTime now, CancellationToken ct);
    Task<User?> FindActiveAsync(Guid id, CancellationToken ct);
}
public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task LogoutAsync(RefreshRequest request, CancellationToken ct);
    Task<UserResponse> MeAsync(CancellationToken ct);
}
