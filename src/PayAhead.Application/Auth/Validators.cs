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
        RuleFor(x => x.LegalVersion).Must(x => x is null || x == LegalDocuments.Version).WithMessage("The legal document version is not current.");
        RuleFor(x => x.Legal).NotNull().WithMessage("Required document confirmations are missing.");
        When(x => x.Legal is not null, () =>
        {
            RuleFor(x => x.Legal!.NoticeAcknowledged).Equal(true);
            RuleFor(x => x.Legal!.PrivacyAcknowledged).Equal(true);
            RuleFor(x => x.Legal!.TermsAccepted).Equal(true);
            RuleFor(x => x.Legal!.NoticeVersion).Equal(LegalDocuments.Version);
            RuleFor(x => x.Legal!.PrivacyVersion).Equal(LegalDocuments.Version);
            RuleFor(x => x.Legal!.TermsVersion).Equal(LegalDocuments.Version);
        });
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
