using PayAhead.Application.Auth;
using PayAhead.Domain;
using PayAhead.Infrastructure;
using Xunit;
namespace PayAhead.UnitTests;
public sealed class PasswordSecurityTests
{
    [Fact] public void Different_salts_do_not_bypass_known_history()
    {
        var hasher = new IdentityPasswordService(); var user = new User(); var previous = "previous-long-password!";
        var first = hasher.Hash(user, previous); var second = hasher.Hash(user, previous); Assert.NotEqual(first, second);
        user.PasswordHash = hasher.Hash(user, "current-long-password!");
        var error = Assert.Throws<PasswordSecurityException>(() => PasswordSecurity.ValidateNew(hasher, user, previous, [first, second])); Assert.Equal("password_reused", error.Code);
        Assert.Throws<PasswordSecurityException>(() => PasswordSecurity.ValidateNew(hasher, user, "current-long-password!", []));
        PasswordSecurity.ValidateNew(hasher, user, "brand-new-long-password!", [first]);
    }
    [Theory][InlineData("password12345")][InlineData("PASSWORD12345")]
    public void Local_common_passwords_are_blocked(string candidate)
    {
        var hasher = new IdentityPasswordService(); var user = new User { PasswordHash = hasher.Hash(new User(), "initial-long-password!") };
        Assert.Equal("password_common", Assert.Throws<PasswordSecurityException>(() => PasswordSecurity.ValidateNew(hasher, user, candidate, [])).Code);
    }
    [Fact] public void Notice_is_versioned_and_legacy_client_is_not_falsely_reported()
    {
        var validator = new RegisterValidator();
        Assert.True(validator.Validate(new RegisterRequest("a@example.invalid", "a-valid-password!", "A")).IsValid);
        Assert.True(validator.Validate(new RegisterRequest("a@example.invalid", "a-valid-password!", "A", LegalDocuments.Version)).IsValid);
        Assert.False(validator.Validate(new RegisterRequest("a@example.invalid", "a-valid-password!", "A", "stale")).IsValid);
    }
}
