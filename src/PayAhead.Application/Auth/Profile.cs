using System.Text.Json.Serialization;
using FluentValidation;
using PayAhead.Application.Payments;

namespace PayAhead.Application.Auth;

public sealed class CurrentPasswordException : Exception;

public sealed record ProfileResponse(Guid Id, string Email, string DisplayName, string RowVersion) { public bool HasAvatar { get; init; } }
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateProfileRequest(string? DisplayName, string? RowVersion);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword, string? Confirmation);
public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100).Must(x => !string.IsNullOrWhiteSpace(x) && !x.Any(char.IsControl));
        RuleFor(x => x.RowVersion).Must(x => { try { return x is not null && x.Length == 12 && Convert.FromBase64String(x).Length == 8; } catch (FormatException) { return false; } });
    }
}
public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).MaximumLength(128).NotEqual(x => x.CurrentPassword).WithMessage("A different password is required.");
        RuleFor(x => x.Confirmation).Equal(x => x.NewPassword).WithMessage("Password confirmation does not match.");
    }
}
public interface IProfileStore
{
    Task<ProfileResponse> ReadAsync(Guid user, CancellationToken ct);
    Task<ProfileResponse> UpdateAsync(Guid user, UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(Guid user, ChangePasswordRequest request, CancellationToken ct);
    Task<SecuritySummary> ReadSecurityAsync(Guid user, CancellationToken ct);
}
public sealed class ProfileService(IProfileStore store, ICurrentUser current, IValidator<UpdateProfileRequest> profile, IValidator<ChangePasswordRequest> password)
{
    private Guid User => current.UserId ?? throw new AuthenticationRequiredException();
    public Task<ProfileResponse> ReadAsync(CancellationToken ct) => store.ReadAsync(User, ct);
    public Task<SecuritySummary> ReadSecurityAsync(CancellationToken ct) => store.ReadSecurityAsync(User, ct);
    public async Task<ProfileResponse> UpdateAsync(UpdateProfileRequest request, CancellationToken ct) { var user = User; await profile.ValidateAndThrowAsync(request, ct); return await store.UpdateAsync(user, request, ct); }
    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct) { var user = User; await password.ValidateAndThrowAsync(request, ct); await store.ChangePasswordAsync(user, request, ct); }
}
