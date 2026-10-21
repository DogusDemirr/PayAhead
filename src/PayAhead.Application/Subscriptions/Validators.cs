using FluentValidation;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Application.Subscriptions;

public sealed class SubscriptionValidator : AbstractValidator<SubscriptionRequest>
{
    public SubscriptionValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150).Must(x => !string.IsNullOrWhiteSpace(x));
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ProviderName).NotEmpty().MaximumLength(100).Must(x => !string.IsNullOrWhiteSpace(x));
        RuleFor(x => x.Amount).NotNull().Must(x => x.HasValue && PaymentPlanRules.IsValidAmount(x.Value));
        RuleFor(x => x.CurrencyCode).NotEmpty().Must(CurrencyCodes.Contains);
        RuleFor(x => x.BillingFrequency).NotNull().Must(x => x is PaymentFrequency.Daily or PaymentFrequency.Weekly or PaymentFrequency.Monthly or PaymentFrequency.Yearly);
        RuleFor(x => x.BillingInterval).NotNull().GreaterThan(0);
        RuleFor(x => x.StartDate).NotNull().Must(x => x.HasValue && x.Value != default);
        RuleFor(x => x.FirstBillingDate).NotNull().Must((r,x) => x.HasValue && x.Value != default && x >= r.StartDate);
        RuleFor(x => x.AnchorDay).InclusiveBetween(1,31).When(x => x.AnchorDay.HasValue);
        RuleFor(x => x.Status).Must(x => x is SubscriptionStatus.Trial or SubscriptionStatus.Active);
        RuleFor(x => x.TrialEndDate).Must((r,end) => r.TrialStartDate.HasValue == end.HasValue && (!end.HasValue || end >= r.TrialStartDate && r.TrialStartDate >= r.StartDate && r.FirstBillingDate > end))
            .WithMessage("Trial tarihleri birlikte, sıralı ve ilk ücretli tarihten önce olmalıdır.");
        RuleFor(x => x.TrialStartDate).NotNull().When(x => x.Status == SubscriptionStatus.Trial);
        RuleFor(x => x.AccessEndsOn).Must((r,end) => !end.HasValue || end >= r.StartDate && (!r.AutoRenew || end >= r.FirstBillingDate))
            .WithMessage("Erişim bitişi başlangıçtan ve otomatik yenilemede ilk tahsilattan önce olamaz.");
        RuleFor(x => x.AccessEndsOn).NotNull().When(x => !x.AutoRenew && x.Status == SubscriptionStatus.Active);
    }
}
public sealed class SubscriptionQueryValidator : AbstractValidator<SubscriptionQuery>
{
    public SubscriptionQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1,1_000_000); RuleFor(x => x.PageSize).InclusiveBetween(1,100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.ProviderName).MaximumLength(100);RuleFor(x => x.Search).MaximumLength(150);
        RuleFor(x => x.CurrencyCode).Must(CurrencyCodes.Contains).When(x => x.CurrencyCode is not null);
    }
}
