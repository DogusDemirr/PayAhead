using FluentValidation;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Application.Occurrences;

public sealed class SetOccurrenceStatusValidator : AbstractValidator<SetOccurrenceStatusRequest>
{
    public SetOccurrenceStatusValidator()
    {
        RuleFor(x => x.Status).NotNull().Must(x => x is null || Enum.IsDefined(x.Value));
        RuleFor(x => x.ActualAmount).Must((request, amount) => request.Status == OccurrenceStatus.Paid
            ? amount is { } value && PaymentPlanRules.IsValidAmount(value) : amount is null)
            .WithMessage("Paid için pozitif, en fazla dört ondalıklı decimal(19,4) tutar zorunludur; diğer durumlarda tutar gönderilemez.");
        RuleFor(x => x.RowVersion).Must(x => x is null || RowVersionToken.IsValid(x)).WithMessage("Geçersiz occurrence rowVersion.");
    }
}

public sealed class OccurrenceListValidator : AbstractValidator<OccurrenceListQuery>
{
    public OccurrenceListValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(x => x is null || Enum.IsDefined(x.Value));
        RuleFor(x => x.ToDate).Must((request, to) => to is null || request.FromDate is null || to >= request.FromDate)
            .WithMessage("ToDate, FromDate'den önce olamaz.");
    }
}
