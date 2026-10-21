using FluentValidation;

namespace PayAhead.Application.Auth;

public static class EmailNormalization
{
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();
}
public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(254)
            .Must(x => x is not null && !x.Trim().Any(char.IsWhiteSpace) && x.Trim().Contains('@'))
            .EmailAddress().WithMessage("Geçerli e-posta gereklidir.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100).Must(x => !string.IsNullOrWhiteSpace(x));
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).MaximumLength(128);
    }
}
public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
public sealed class RefreshValidator : AbstractValidator<RefreshRequest>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty().Length(64)
        .Matches("^[A-Za-z0-9_-]{64}$").WithMessage("Geçerli refresh token gereklidir.");
}
