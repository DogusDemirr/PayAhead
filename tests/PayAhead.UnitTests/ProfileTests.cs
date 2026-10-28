using PayAhead.Application.Auth;
using Xunit;

namespace PayAhead.UnitTests;
public sealed class ProfileTests
{
    [Theory][InlineData("")][InlineData(" ")][InlineData("name\nline")]
    public void Profile_rejects_blank_and_control_characters(string name) => Assert.False(new UpdateProfileValidator().Validate(new UpdateProfileRequest(name, Convert.ToBase64String(new byte[8]))).IsValid);
    [Fact] public void Profile_validates_length_and_concurrency_token()
    {
        var validator = new UpdateProfileValidator(); Assert.True(validator.Validate(new UpdateProfileRequest("Test user", Convert.ToBase64String(new byte[8]))).IsValid);
        Assert.False(validator.Validate(new UpdateProfileRequest(new string('a', 101), Convert.ToBase64String(new byte[8]))).IsValid); Assert.False(validator.Validate(new UpdateProfileRequest("Name", "invalid")).IsValid);
    }
    [Fact] public void Password_requires_confirmation_policy_and_a_different_password()
    {
        var v = new ChangePasswordValidator(); Assert.True(v.Validate(new ChangePasswordRequest("old-password!", "new-password!", "new-password!")).IsValid);
        Assert.False(v.Validate(new ChangePasswordRequest("old-password!", "short", "short")).IsValid); Assert.False(v.Validate(new ChangePasswordRequest("old-password!", "old-password!", "old-password!")).IsValid); Assert.False(v.Validate(new ChangePasswordRequest("old-password!", "new-password!", "mismatch")).IsValid);
    }
    [Fact] public void Validation_messages_never_echo_password_values()
    {
        var request = new ChangePasswordRequest("secret-old-password", "secret-old-password", "secret-confirmation");
        var errors = string.Join(" ", new ChangePasswordValidator().Validate(request).Errors.Select(x => x.ErrorMessage));
        Assert.DoesNotContain(request.CurrentPassword!, errors); Assert.DoesNotContain(request.Confirmation!, errors);
    }
}
