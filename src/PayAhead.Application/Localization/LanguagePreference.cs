using System.Text.Json.Serialization;
using FluentValidation;
using PayAhead.Application.Payments;
namespace PayAhead.Application.Localization;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LanguagePreferenceRequest(string? LanguageCode);
public sealed record LanguagePreferenceResponse(string LanguageCode);
public interface ILanguagePreferenceStore
{
    Task<LanguagePreferenceResponse> ReadAsync(Guid user, CancellationToken ct);
    Task<LanguagePreferenceResponse> WriteAsync(Guid user, string language, DateTime now, CancellationToken ct);
}
public sealed class LanguagePreferenceService(ILanguagePreferenceStore store, ICurrentUser current, TimeProvider clock)
{
    private Guid User => current.UserId ?? throw new AuthenticationRequiredException();
    public Task<LanguagePreferenceResponse> ReadAsync(CancellationToken ct) => store.ReadAsync(User, ct);
    public Task<LanguagePreferenceResponse> WriteAsync(LanguagePreferenceRequest request, CancellationToken ct)
    {
        var user = User;
        if (!SupportedLanguages.Contains(request.LanguageCode)) throw new ValidationException([new FluentValidation.Results.ValidationFailure("LanguageCode", "Unsupported language code.")]);
        return store.WriteAsync(user, request.LanguageCode!, clock.GetUtcNow().UtcDateTime, ct);
    }
}
