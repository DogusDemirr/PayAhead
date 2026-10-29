using FluentValidation;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Application.Auth;

public sealed class AuthService(IAuthStore store, IPasswordService passwords, ITokenService tokens,
    ICurrentUser currentUser, TimeProvider clock, IValidator<RegisterRequest> registration,
    IValidator<LoginRequest> login, IValidator<RefreshRequest> refresh) : IAuthService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        await registration.ValidateAndThrowAsync(request, ct);
        if (PasswordSecurity.IsCommon(request.Password!)) throw new PasswordSecurityException("password_common");
        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email!.Trim(),
            NormalizedEmail = EmailNormalization.Normalize(request.Email),
            DisplayName = request.DisplayName!.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        user.PasswordHash = passwords.Hash(user, request.Password!);
        await store.RegisterAsync(user, ct, request.LegalVersion);
        return UserResponse.From(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        await login.ValidateAndThrowAsync(request, ct);
        var now = clock.GetUtcNow().UtcDateTime;
        var user = await store.AuthenticateAsync(EmailNormalization.Normalize(request.Email!), candidate =>
        {
            var valid = passwords.Verify(candidate, request.Password!, out var rehash);
            if (valid && rehash) candidate.PasswordHash = passwords.Hash(candidate, request.Password!);
            return valid;
        }, now, ct);
        if (user is null) throw new AuthenticationRequiredException();
        var raw = tokens.CreateRefreshToken();
        var session = NewRefresh(raw, now);
        session.UserId = user.Id;
        await store.AddSessionAsync(session, ct, user.SecurityStamp);
        return Response(user, raw, session);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        await refresh.ValidateAndThrowAsync(request, ct);
        var raw = tokens.CreateRefreshToken();
        var replacement = NewRefresh(raw, clock.GetUtcNow().UtcDateTime);
        var user = await store.RotateAsync(tokens.HashRefreshToken(request.RefreshToken!), replacement,
            replacement.CreatedAtUtc, ct);
        if (user is null) throw new AuthenticationRequiredException();
        return Response(user, raw, replacement);
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken ct)
    {
        await refresh.ValidateAndThrowAsync(request, ct);
        await store.RevokeAsync(tokens.HashRefreshToken(request.RefreshToken!), clock.GetUtcNow().UtcDateTime, ct);
    }

    public async Task<UserResponse> MeAsync(CancellationToken ct)
        => UserResponse.From(await store.FindActiveAsync(currentUser.UserId ?? Guid.Empty, ct)
            ?? throw new AuthenticationRequiredException());

    private RefreshToken NewRefresh(string raw, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        FamilyId = Guid.NewGuid(),
        TokenHash = tokens.HashRefreshToken(raw),
        CreatedAtUtc = now,
        ExpiresAtUtc = now.AddDays(tokens.RefreshLifetimeDays)
    };
    private AuthResponse Response(User user, string raw, RefreshToken session)
    {
        var access = tokens.CreateAccessToken(user.Id, user.SecurityStamp);
        return new(access.Token, raw, access.ExpiresAtUtc, session.ExpiresAtUtc, UserResponse.From(user));
    }
}
