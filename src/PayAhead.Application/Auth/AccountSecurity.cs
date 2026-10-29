using PayAhead.Domain;

namespace PayAhead.Application.Auth;

public sealed class PasswordSecurityException(string code) : Exception { public string Code { get; } = code; }
public sealed record SecuritySummary(int RemainingChanges, int ChangeLimit, int WindowDays, DateTime? NextAvailableAtUtc, DateTime AsOfUtc, int ActiveSessions);
public static class PasswordSecurity
{
    public const int Limit = 3;
    public const int WindowDays = 30;
    // Deliberately small, local denylist. No password or hash is sent to a third party.
    // This is not a comprehensive breach database or a NIST compliance claim.
    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    { "password", "password123", "password1234", "password12345", "123456789012", "123456789012345", "qwerty123456", "qwerty123456789", "letmein123456", "admin123456789", "payahead12345", "payahead123456", "passwordpassword" };
    public static bool IsCommon(string password) => Common.Contains(password);
    public static void ValidateNew(IPasswordService passwords, User user, string candidate, IEnumerable<string> retired)
    {
        if (IsCommon(candidate)) throw new PasswordSecurityException("password_common");
        if (passwords.Verify(user, candidate, out _) || retired.Any(hash => passwords.VerifyStored(user, hash, candidate)))
            throw new PasswordSecurityException("password_reused");
    }
}
public static class LegalDocuments
{
    public const string Version = "2026-10-29-draft-1";
    public static object Manifest => new { version = Version, isDraft = true, requiresTermsAcceptance = false, documentIds = new[] { "notice", "privacy", "terms" } };
}
