using FluentValidation;
using PayAhead.Domain;

namespace PayAhead.Application.Payments;

public static class RowVersionToken
{
    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != 12) return false;
        Span<byte> bytes = stackalloc byte[8];
        return Convert.TryFromBase64String(value, bytes, out var written) && written == 8
            && Convert.ToBase64String(bytes) == value;
    }
}

public class PaymentWriteValidator<T> : AbstractValidator<T> where T : PaymentWriteRequest
{
    public PaymentWriteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Amount).NotNull().Must(x => x is null || PaymentPlanRules.IsValidAmount(x.Value))
            .WithMessage("Tutar pozitif, decimal(19,4) sınırlarında ve en fazla dört ondalıklı olmalıdır.");
        RuleFor(x => x.CurrencyCode).NotEmpty().Must(CurrencyCodes.Contains).WithMessage("Geçerli ISO 4217 kodu gereklidir.");
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Frequency).NotNull().Must(x => x is null || Enum.IsDefined(x.Value)).WithMessage("Geçersiz sıklık.");
        RuleFor(x => x.Interval).NotNull().GreaterThanOrEqualTo(1);
        RuleFor(x => x.StartDate).NotNull().Must(x => x is null || x.Value != default).WithMessage("Geçerli başlangıç tarihi gereklidir.");
        RuleFor(x => x.EndDate).Must((request, end) => end is null || request.StartDate is null || end >= request.StartDate)
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.");
        RuleFor(x => x.AnchorDay).Must(x => x is null or >= 1 and <= 31).WithMessage("AnchorDay 1 ile 31 arasında olmalıdır.");
    }
}

public sealed class CreatePaymentValidator : PaymentWriteValidator<CreatePaymentRequest>;
public sealed class UpdatePaymentValidator : PaymentWriteValidator<UpdatePaymentRequest>
{
    public UpdatePaymentValidator() => RuleFor(x => x.RowVersion).Must(RowVersionToken.IsValid)
        .WithMessage("Sekiz baytlık SQL rowversion değerinin Base64 gösterimi gereklidir.");
}

public sealed class PaymentListValidator : AbstractValidator<PaymentListQuery>
{
    public PaymentListValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Category).MaximumLength(50).Must(x => x is null || !string.IsNullOrWhiteSpace(x));
        RuleFor(x => x.CurrencyCode).Must(x => x is null || CurrencyCodes.Contains(x)).WithMessage("Geçersiz para birimi filtresi.");
        RuleFor(x => x.Search).MaximumLength(150).Must(x => x is null || !string.IsNullOrWhiteSpace(x));
    }
}
