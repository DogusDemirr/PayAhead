using PayAhead.Application.Auth;
using Xunit;
namespace PayAhead.UnitTests;
public sealed class RegistrationLegalTests
{
    [Theory]
    [InlineData("notice", false)][InlineData("privacy", false)][InlineData("terms", false)]
    [InlineData("notice", true)][InlineData("privacy", true)][InlineData("terms", true)]
    public void Every_confirmation_and_current_document_version_is_required(string id, bool invalidVersion)
    {
        var legal = TestLegal.Confirmed;
        legal = (id, invalidVersion) switch
        {
            ("notice", false) => legal with { NoticeAcknowledged = false }, ("privacy", false) => legal with { PrivacyAcknowledged = false }, ("terms", false) => legal with { TermsAccepted = false },
            ("notice", true) => legal with { NoticeVersion = "stale" }, ("privacy", true) => legal with { PrivacyVersion = null }, _ => legal with { TermsVersion = "" }
        };
        Assert.False(new RegisterValidator().Validate(new RegisterRequest("test@example.invalid", "valid-password-123!", "Test", Legal: legal)).IsValid);
    }
    [Fact] public void Presentation_metadata_alone_is_not_a_confirmation_and_optional_consent_is_not_required()
    {
        var request = new RegisterRequest("test@example.invalid", "valid-password-123!", "Test", LegalDocuments.Version);
        Assert.False(new RegisterValidator().Validate(request).IsValid);
        Assert.True(new RegisterValidator().Validate(request with { Legal = TestLegal.Confirmed }).IsValid);
    }
}
