using FluentValidation;
using PayAhead.Application.Payments;

namespace PayAhead.Application.Overview;

public class OverviewRangeValidator<T> : AbstractValidator<T> where T : OverviewRangeQuery
{
    public OverviewRangeValidator()
    {
        RuleFor(x => x.ToDate).Must((query, to) => query.FromDate is null || to is null ||
            to.Value.DayNumber >= query.FromDate.Value.DayNumber && to.Value.DayNumber - query.FromDate.Value.DayNumber < OverviewLimits.MaximumDays)
            .WithMessage("Tarih aralığı sıralı olmalı ve en fazla 366 dahil gün içermelidir.");
    }
}
public sealed class SummaryQueryValidator : OverviewRangeValidator<OverviewRangeQuery>;
public sealed class UpcomingPaymentsValidator : OverviewRangeValidator<UpcomingPaymentsQuery>
{
    public UpcomingPaymentsValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.CurrencyCode).Must(x => x is null || CurrencyCodes.Contains(x)).WithMessage("Geçersiz ISO 4217 kodu.");
        RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
public sealed class CalendarQueryValidator : AbstractValidator<CalendarQuery>
{
    public CalendarQueryValidator()
    {
        RuleFor(x => x.Year).NotNull().InclusiveBetween(1, 9999);
        RuleFor(x => x.Month).NotNull().InclusiveBetween(1, 12);
        RuleFor(x => x.Status).Must(x => x is null || Enum.IsDefined(x.Value));
        RuleFor(x => x.CurrencyCode).Must(x => x is null || CurrencyCodes.Contains(x)).WithMessage("Geçersiz ISO 4217 kodu.");
    }
}
