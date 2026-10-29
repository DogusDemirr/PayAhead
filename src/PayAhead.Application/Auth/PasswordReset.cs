using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using PayAhead.Domain;

namespace PayAhead.Application.Auth;

public sealed class PasswordResetUnavailableException : Exception;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ForgotPasswordRequest(string? Email);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResetPasswordRequest(string? Token, string? Password);
public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
}
public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().Length(43).Matches("^[A-Za-z0-9_-]{43}$");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
    }
}
public sealed class PasswordResetOptions
{
    public int LifetimeMinutes { get; set; } = 15;
    public string LinkBase { get; set; } = "";
}
public interface IPasswordResetEmail
{
    Task<bool> SendAsync(string email, string link, int lifetimeMinutes, CancellationToken ct);
}
public sealed record PasswordResetDelivery(Guid TokenId, string Email, string Link, int LifetimeMinutes, DateTime ExpiresAtUtc);
public interface IPasswordResetDispatcher
{
    bool TryEnqueue(PasswordResetDelivery delivery);
}
public interface IPasswordResetStore
{
    Task<User?> IssueAsync(string normalizedEmail, PasswordResetToken token, CancellationToken ct);
    Task<bool> ConsumeAsync(string hash, DateTime now, Func<User, IReadOnlyList<string>, string> newHash, CancellationToken ct);
    Task InvalidateAsync(Guid id, DateTime now, CancellationToken ct);
}
public sealed class PasswordResetService(IPasswordResetStore store, IPasswordService passwords,
    IPasswordResetDispatcher dispatcher, PasswordResetOptions options, TimeProvider clock,
    IValidator<ForgotPasswordRequest> forgotValidator, IValidator<ResetPasswordRequest> resetValidator)
{
    public static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    public async Task ForgotAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        await forgotValidator.ValidateAndThrowAsync(request, ct);
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var now = clock.GetUtcNow().UtcDateTime;
        var token = new PasswordResetToken { Id = Guid.NewGuid(), TokenHash = Hash(raw), CreatedAtUtc = now, ExpiresAtUtc = now.AddMinutes(options.LifetimeMinutes) };
        // No links are constructed from client-controlled hosts or redirect parameters.
        var user = await store.IssueAsync(EmailNormalization.Normalize(request.Email!), token, ct);
        if (user is null) return;
        var link = options.LinkBase + (options.LinkBase.Contains('?') ? "&" : "?") + "token=" + raw;
        // SMTP latency/failure must not reveal account existence through the HTTP response time.
        if (!dispatcher.TryEnqueue(new(token.Id, user.Email, link, options.LifetimeMinutes, token.ExpiresAtUtc)))
            await store.InvalidateAsync(token.Id, now, ct);
    }
    public async Task ResetAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        await resetValidator.ValidateAndThrowAsync(request, ct);
        if (!await store.ConsumeAsync(Hash(request.Token!), clock.GetUtcNow().UtcDateTime, (user, retired) => { PasswordSecurity.ValidateNew(passwords, user, request.Password!, retired); return passwords.Hash(user, request.Password!); }, ct))
            throw new PayAhead.Domain.DomainRuleException("Bağlantı geçersiz, kullanılmış veya süresi dolmuş. Yeni bir bağlantı iste.");
    }
}
