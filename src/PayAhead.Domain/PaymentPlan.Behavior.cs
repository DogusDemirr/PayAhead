namespace PayAhead.Domain;

public sealed class DomainRuleException(string message) : Exception(message);

public sealed record PaymentPlanDetails(string Name, string? Description, decimal Amount, string CurrencyCode,
    string Category, PaymentFrequency Frequency, int Interval, DateOnly StartDate, DateOnly? EndDate,
    int? AnchorDay, bool IsActive);

public static class PaymentPlanRules
{
    public const decimal MaximumAmount = 999999999999999.9999m;
    public static bool IsValidAmount(decimal amount)
        => amount > 0 && amount <= MaximumAmount && decimal.Round(amount, 4) == amount;
}

public sealed partial class PaymentPlan
{
    public static PaymentPlan Create(Guid userId, PaymentPlanDetails details, DateTime nowUtc)
    {
        if (userId == Guid.Empty) throw new DomainRuleException("Geçerli kullanıcı gereklidir.");
        var plan = new PaymentPlan { Id = Guid.NewGuid(), UserId = userId, CreatedAtUtc = nowUtc };
        plan.UpdateDetails(details, nowUtc);
        return plan;
    }

    public void UpdateDetails(PaymentPlanDetails details, DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc) throw new DomainRuleException("Zaman damgası UTC olmalıdır.");
        if (string.IsNullOrWhiteSpace(details.Name) || details.Name.Length > 150
            || details.Description?.Length > 2000 || string.IsNullOrWhiteSpace(details.Category) || details.Category.Length > 50
            || !PaymentPlanRules.IsValidAmount(details.Amount) || !Enum.IsDefined(details.Frequency)
            || details.Interval < 1 || details.StartDate == default || details.EndDate < details.StartDate
            || details.AnchorDay is < 1 or > 31 || details.CurrencyCode is null
            || details.CurrencyCode.Length != 3 || details.CurrencyCode.Any(c => c is < 'A' or > 'Z'))
            throw new DomainRuleException("Ödeme planı alanları geçersiz.");

        Name = details.Name; Description = details.Description; Amount = details.Amount;
        CurrencyCode = details.CurrencyCode; Category = details.Category; Frequency = details.Frequency;
        Interval = details.Interval; StartDate = details.StartDate; EndDate = details.EndDate;
        AnchorDay = details.AnchorDay; IsActive = details.IsActive; UpdatedAtUtc = nowUtc;
    }
}
